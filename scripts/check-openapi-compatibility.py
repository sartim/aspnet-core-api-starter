#!/usr/bin/env python3
"""Fail when an OpenAPI change removes an existing client guarantee.

The baseline may be a complete OpenAPI document or the repository's compact
route snapshot. Complete documents additionally get schema compatibility
checks for paths, parameters, responses, and component schemas.
"""

from __future__ import annotations

import json
import sys
import urllib.request
from pathlib import Path
from typing import Any

HTTP_METHODS = {"delete", "get", "head", "options", "patch", "post", "put", "trace"}


def load_document(value: str) -> dict[str, Any]:
    if value.startswith(("http://", "https://")):
        with urllib.request.urlopen(value, timeout=30) as response:
            return json.load(response)
    return json.loads(Path(value).read_text(encoding="utf-8"))


def routes(document: dict[str, Any]) -> dict[str, dict[str, Any]]:
    if isinstance(document, list):
        return {
            f"{route['method'].upper()} {route['path']}": {}
            for route in document
        }
    if "paths" in document:
        return {
            f"{method.upper()} {path}": operation
            for path, item in document.get("paths", {}).items()
            for method, operation in item.items()
            if method.lower() in HTTP_METHODS and isinstance(operation, dict)
        }
    return {
        f"{route['method'].upper()} {route['path']}": {}
        for route in document.get("routes", [])
    }


def schema_ref(schema: dict[str, Any], document: dict[str, Any]) -> dict[str, Any]:
    ref = schema.get("$ref")
    if not ref or not ref.startswith("#/components/schemas/"):
        return schema
    current: Any = document
    for part in ref[2:].split("/"):
        current = current.get(part, {})
    return current if isinstance(current, dict) else {}


def compare_schema(old: dict[str, Any], new: dict[str, Any], old_document: dict[str, Any], new_document: dict[str, Any], location: str, errors: list[str]) -> None:
    old = schema_ref(old, old_document)
    new = schema_ref(new, new_document)
    if old.get("type") and new.get("type") and old["type"] != new["type"]:
        errors.append(f"{location}: schema type changed from {old['type']} to {new['type']}")

    old_enum = set(old.get("enum", []))
    new_enum = set(new.get("enum", []))
    for value in sorted(old_enum - new_enum, key=str):
        errors.append(f"{location}: enum value removed: {value}")

    old_properties = old.get("properties", {})
    new_properties = new.get("properties", {})
    for name in sorted(set(old_properties) - set(new_properties)):
        errors.append(f"{location}: response/request property removed: {name}")
    for name in sorted(set(old_properties) & set(new_properties)):
        compare_schema(old_properties[name], new_properties[name], old_document, new_document, f"{location}.{name}", errors)

    old_required = set(old.get("required", []))
    new_required = set(new.get("required", []))
    for name in sorted(new_required - old_required):
        if name in old_properties or name in new_properties:
            errors.append(f"{location}: property became required: {name}")


def compare_operation(old: dict[str, Any], new: dict[str, Any], baseline: dict[str, Any], current: dict[str, Any], location: str, errors: list[str]) -> None:
    old_parameters = {(p.get("in"), p.get("name")): p for p in old.get("parameters", [])}
    new_parameters = {(p.get("in"), p.get("name")): p for p in new.get("parameters", [])}
    for key in sorted(set(old_parameters) - set(new_parameters)):
        errors.append(f"{location}: parameter removed: {key[0]} {key[1]}")
    for key in sorted(set(old_parameters) & set(new_parameters)):
        old_parameter = old_parameters[key]
        new_parameter = new_parameters[key]
        if old_parameter.get("required") and not new_parameter.get("required"):
            continue
        if not old_parameter.get("required") and new_parameter.get("required"):
            errors.append(f"{location}: optional parameter became required: {key[0]} {key[1]}")
        old_schema = old_parameter.get("schema", {})
        new_schema = new_parameter.get("schema", {})
        compare_schema(old_schema, new_schema, baseline, current, f"{location} parameter {key[1]}", errors)

    old_body = old.get("requestBody", {})
    new_body = new.get("requestBody", {})
    if old_body.get("required") and not new_body.get("required"):
        pass
    elif not old_body.get("required") and new_body.get("required"):
        errors.append(f"{location}: optional request body became required")
    for content_type, old_content in old_body.get("content", {}).items():
        new_content = new_body.get("content", {}).get(content_type)
        if new_content is None:
            errors.append(f"{location}: request content type removed: {content_type}")
        elif "schema" in old_content and "schema" in new_content:
            compare_schema(old_content["schema"], new_content["schema"], baseline, current, f"{location} request body", errors)

    old_responses = old.get("responses", {})
    new_responses = new.get("responses", {})
    for status in sorted(set(old_responses) - set(new_responses)):
        errors.append(f"{location}: response removed: {status}")
    for status in sorted(set(old_responses) & set(new_responses)):
        for content_type, old_content in old_responses[status].get("content", {}).items():
            new_content = new_responses[status].get("content", {}).get(content_type)
            if new_content is None:
                errors.append(f"{location} response {status}: content type removed: {content_type}")
            elif "schema" in old_content and "schema" in new_content:
                compare_schema(old_content["schema"], new_content["schema"], baseline, current, f"{location} response {status}", errors)


def main() -> int:
    if len(sys.argv) != 3:
        print(f"usage: {sys.argv[0]} CURRENT_OPENAPI BASELINE_OPENAPI", file=sys.stderr)
        return 2
    current = load_document(sys.argv[1])
    baseline = load_document(sys.argv[2])
    current_routes = routes(current)
    baseline_routes = routes(baseline)
    errors = [f"route removed: {route}" for route in sorted(set(baseline_routes) - set(current_routes))]
    for route in sorted(set(baseline_routes) & set(current_routes)):
        if baseline_routes[route] and current_routes[route]:
            compare_operation(baseline_routes[route], current_routes[route], baseline, current, route, errors)

    for name, old_schema in baseline.get("components", {}).get("schemas", {}).items():
        new_schema = current.get("components", {}).get("schemas", {}).get(name)
        if new_schema is None:
            errors.append(f"component schema removed: {name}")
        else:
            compare_schema(old_schema, new_schema, baseline, current, f"component {name}", errors)

    if errors:
        print("OpenAPI compatibility check failed:")
        print("\n".join(f"- {error}" for error in errors))
        return 1
    print(f"OpenAPI compatibility check passed ({len(baseline_routes)} baseline routes retained).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
