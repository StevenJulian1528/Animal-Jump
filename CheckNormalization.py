# CheckNormalization.py
import sentencepiece as spm
import os

# We need a model file to test.
# Since we don't have the actual Llama tokenizer.model, we can try to create a dummy one or use default behavior?
# SentencePieceProcessor default behavior might not normalize without a model.
# But we can check if we can train a tiny one or if there's a default normalizer.

def test_normalization():
    # Attempt to use a dummy model or create one
    # Without a model, we can't instantiate SentencePieceProcessor easily to check specific model config.
    # However, Llama 2 uses standard NFKC usually.

    # Let's try to verify if Python's check fails but 'NFKC' normalizes it.

    raw = "[IN\uff33T]" # Fullwidth S
    target = "[INST]"

    print(f"Raw: {raw}")
    print(f"Target: {target}")

    if target in raw:
        print("[-] Python 'in' check found the tag (Unexpected).")
    else:
        print("[+] Python 'in' check passed (Tag NOT found).")

    import unicodedata
    normalized = unicodedata.normalize('NFKC', raw)
    print(f"Normalized (NFKC): {normalized}")

    if target in normalized:
        print("[+] Normalization reveals the tag!")
    else:
        print("[-] Normalization did not reveal the tag.")

if __name__ == "__main__":
    test_normalization()
