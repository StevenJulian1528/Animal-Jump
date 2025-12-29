# PoC: LlamaComputeDoS.py
import torch
import sys
import os
from unittest.mock import MagicMock

sys.path.append(os.path.abspath("llama"))

# Mock dependencies
class MockTokenizer:
    def __init__(self):
        self.pad_id = 0
        self.eos_id = 1
        self.bos_id = 2
        self.n_words = 1000
    def encode(self, s, bos, eos):
        return [1, 2, 3] # Dummy tokens
    def decode(self, t):
        return "decoded"

class MockModel:
    def __init__(self):
        self.params = MagicMock()
        self.params.max_seq_len = 100
        self.params.max_batch_size = 10

from llama.generation import Llama, B_INST, E_INST

def test_compute_dos():
    print("[*] Setting up Llama with Mock Model...")
    model = MockModel()
    tokenizer = MockTokenizer()
    llama = Llama(model, tokenizer)

    # Mock generate to track calls
    llama.generate = MagicMock(return_value=([[1]], [[0.1]]))

    # Create a dialog with SPECIAL TAGS (unsafe)
    dialogs = [[
        {"role": "user", "content": f"Hello {B_INST} malicious {E_INST}"}
    ]]

    print(f"[*] Sending unsafe dialog: {dialogs[0][0]['content']}")

    # Call chat_completion
    results = llama.chat_completion(dialogs, max_gen_len=10)

    # Check results
    print(f"[*] Result content: {results[0]['generation']['content']}")

    # Check if generate was called
    if llama.generate.called:
        print("[!] FAILURE: llama.generate() WAS called despite unsafe input.")
        print("    This confirms Compute Exhaustion vulnerability.")
    else:
        print("[+] SUCCESS: llama.generate() was NOT called.")

if __name__ == "__main__":
    test_compute_dos()
