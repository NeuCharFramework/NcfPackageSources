"""Generate random tiny weights locally, for infrastructure tests only."""

import argparse
from pathlib import Path


def create(root, backend="cpu"):
    from tokenizers import Tokenizer
    from tokenizers.models import WordLevel
    from tokenizers.pre_tokenizers import Whitespace
    from transformers import GPT2Config, GPT2LMHeadModel, LlamaConfig, LlamaForCausalLM, PreTrainedTokenizerFast

    path = Path(root) / ("tiny-llama" if backend == "mlx" else "tiny-gpt2")
    path.mkdir(parents=True, exist_ok=True)
    words = ["<pad>", "<eos>", "<unk>", "Question", "Answer", "user", "assistant", "system", ":"]
    words += [str(index) for index in range(32)]
    vocabulary = {word: index for index, word in enumerate(words)}
    tokenizer = Tokenizer(WordLevel(vocabulary, unk_token="<unk>"))
    tokenizer.pre_tokenizer = Whitespace()
    fast = PreTrainedTokenizerFast(tokenizer_object=tokenizer, pad_token="<pad>", eos_token="<eos>", unk_token="<unk>")
    fast.chat_template = "{% for message in messages %}{{ message['role'] + ': ' + message['content'] + '\\n' }}{% endfor %}{{ eos_token }}"
    fast.save_pretrained(path)
    if backend == "mlx":
        model = LlamaForCausalLM(LlamaConfig(
            vocab_size=len(vocabulary), hidden_size=64, intermediate_size=128, num_hidden_layers=1,
            num_attention_heads=4, num_key_value_heads=2, max_position_embeddings=128,
            pad_token_id=0, eos_token_id=1, bos_token_id=1,
        ))
    else:
        model = GPT2LMHeadModel(GPT2Config(
            vocab_size=len(vocabulary), n_positions=128, n_embd=32, n_layer=1, n_head=2,
            pad_token_id=0, eos_token_id=1, bos_token_id=1,
        ))
    model.save_pretrained(path, safe_serialization=True)
    return path.name


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("root")
    parser.add_argument("--backend", choices=["cpu", "mlx"], default="cpu")
    args = parser.parse_args()
    print(create(args.root, args.backend))
