# Vulnerability Reproduction: Heap Buffer Overflow in GLES2DecoderImpl

This repository contains reproduction artifacts for the Heap Buffer Overflow vulnerability in `gpu::gles2::GLES2DecoderImpl::DecompressTextureData`.

## Contents

- `repro_issue.cc`: A standalone C++ program that simulates the vulnerable logic in `DecompressTextureData`. It mocks the `base::HeapArray` allocation and the decompression function to demonstrate the overflow.
- `poc.html`: A Proof-of-Concept HTML file using WebGL 2 to trigger the vulnerable code path in a browser (requires an environment where the vulnerability is present and software decompression is used).

## How to Reproduce (C++ Simulation)

1. **Compile with AddressSanitizer (ASAN):**
   You need `clang++` installed.
   ```bash
   clang++ -fsanitize=address -g -O0 repro_issue.cc -o repro_issue
   ```

2. **Run the executable:**
   ```bash
   ./repro_issue
   ```

   **Expected Output:**
   ASAN should report a `heap-buffer-overflow` because the code allocates memory for a single 2D slice (`width * height`) but writes data for all depth layers (`width * height * depth`).

## Recommended Fix

The vulnerability is caused by ignoring the `depth` parameter when calculating the allocation size.

**Vulnerable Code:**
```cpp
auto decompressed_data =
    base::HeapArray<uint8_t>::Uninit(output_pixel_size * width * height);
```

**Fixed Code:**
Use `base::CheckedNumeric` to safely calculate the size including `depth`.

```cpp
base::CheckedNumeric<size_t> safe_size = output_pixel_size;
safe_size *= width;
safe_size *= height;
safe_size *= depth; // Include depth!

if (!safe_size.IsValid()) {
  // Handle error (return empty array or signal OOM)
  return base::HeapArray<uint8_t>();
}

auto decompressed_data =
    base::HeapArray<uint8_t>::Uninit(safe_size.ValueOrDie());
```
