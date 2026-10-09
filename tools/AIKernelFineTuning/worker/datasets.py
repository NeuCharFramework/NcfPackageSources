import hashlib
import json
import random

from .config import MAX_DATASET_BYTES, WorkerError


def parse_json(text):
    def pairs(items):
        result = {}
        for key, value in items:
            if key in result:
                raise ValueError(f"Duplicate property: {key}")
            result[key] = value
        return result

    def invalid(value):
        raise ValueError(f"Non-finite JSON number: {value}")

    return json.loads(text, object_pairs_hook=pairs, parse_constant=invalid)


def validate_dataset(content):
    if not isinstance(content, str) or not content.strip():
        raise WorkerError("Dataset content must be nonempty JSONL.")
    if len(content.encode("utf-8")) > MAX_DATASET_BYTES:
        raise WorkerError("Dataset exceeds the 2 MiB UTF-8 limit.", 413)
    rows, errors = [], []
    lines = content.splitlines()
    if len(lines) > 10000:
        raise WorkerError("Dataset exceeds the 10000 row limit.")
    for number, line in enumerate(lines, 1):
        try:
            row = parse_json(line)
            if not isinstance(row, dict):
                raise ValueError("Row must be an object.")
            if set(row) == {"prompt", "completion"}:
                check_text(row["prompt"], "prompt")
                check_text(row["completion"], "completion")
            elif set(row) == {"messages"}:
                messages = row["messages"]
                if not isinstance(messages, list) or not 2 <= len(messages) <= 128:
                    raise ValueError("messages must contain 2..128 entries.")
                expected = "user"
                for index, message in enumerate(messages):
                    if not isinstance(message, dict) or set(message) != {"role", "content"}:
                        raise ValueError("Each message requires only role and content.")
                    role = message["role"]
                    if index == 0 and role == "system":
                        check_text(message["content"], "content")
                        continue
                    if role != expected:
                        raise ValueError(f"Expected {expected} role at message {index + 1}; system is allowed only first.")
                    check_text(message["content"], "content")
                    expected = "assistant" if role == "user" else "user"
                if expected != "user":
                    raise ValueError("Conversation must end with an assistant response.")
            else:
                raise ValueError("Use only messages OR prompt/completion properties.")
            rows.append(row)
        except (ValueError, TypeError, RecursionError) as exc:
            if len(errors) < 50:
                errors.append({"row": number, "error": str(exc)})
    if errors:
        raise WorkerError("Invalid JSONL dataset (first 50 row errors).", details=errors)
    if len(rows) < 2:
        raise WorkerError("Dataset must contain at least two rows.")
    canonical = "\n".join(json.dumps(row, ensure_ascii=False, separators=(",", ":")) for row in rows)
    return rows, canonical, hashlib.sha256(canonical.encode("utf-8")).hexdigest()


def check_text(value, field):
    if not isinstance(value, str) or not value.strip() or len(value) > 65536 or "\x00" in value:
        raise ValueError(f"{field} must be nonempty text, at most 65536 characters, without NUL.")


def row_key(row):
    return json.dumps(row, sort_keys=True, ensure_ascii=False, separators=(",", ":"))


def split_rows(training, evaluation, seed):
    unique = {row_key(row): row for row in training}
    if evaluation is not None:
        if set(unique).intersection(row_key(row) for row in evaluation):
            raise WorkerError("Training and evaluation datasets overlap; held-out evaluation must be independent.")
        return training, evaluation
    if len(unique) < 2:
        raise WorkerError("Automatic held-out evaluation requires at least two distinct rows.")
    groups = list(unique)
    random.Random(seed).shuffle(groups)
    held_out = set(groups[:max(1, len(groups) // 5)])
    return (
        [row for row in training if row_key(row) not in held_out],
        [row for row in training if row_key(row) in held_out],
    )


def encode_row(tokenizer, row, max_length):
    if "messages" in row:
        if getattr(tokenizer, "chat_template", None):
            tokens = tokenizer.apply_chat_template(row["messages"], tokenize=True, add_generation_prompt=False)
        else:
            raise WorkerError("messages datasets require an approved tokenizer chat_template; use prompt/completion for models without one.")
    else:
        tokens = tokenizer.encode(row["prompt"] + "\n" + row["completion"], add_special_tokens=True)
    if tokenizer.eos_token_id is not None and (not tokens or tokens[-1] != tokenizer.eos_token_id):
        tokens.append(tokenizer.eos_token_id)
    tokens = tokens[:max_length]
    if len(tokens) < 2:
        raise WorkerError("Dataset row produces fewer than two tokens with this model tokenizer.")
    return tokens
