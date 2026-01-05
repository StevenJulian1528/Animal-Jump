#include <iostream>
#include <vector>
#include <cstdint>
#include <cstring>
#include <cassert>

// Mocking base::HeapArray
namespace base {
template <typename T>
struct HeapArray {
    // In Chromium this is a smart pointer owning an array.
    // We use a raw pointer here to ensure ASAN catches the exact allocation size.
    // std::vector might round up allocation or add metadata that confuses simple offset checks,
    // although ASAN usually handles vector fine. Let's use new[] to be closer to "HeapArray".
    T* buffer_ptr;
    size_t size_bytes;

    HeapArray() : buffer_ptr(nullptr), size_bytes(0) {}

    static HeapArray Uninit(size_t size) {
        HeapArray arr;
        arr.size_bytes = size;
        arr.buffer_ptr = new T[size]; // ASAN tracks this allocation
        std::cout << "[DecompressTextureData] Allocated Buffer Size: " << size << " bytes" << std::endl;
        return arr;
    }

    T* data() { return buffer_ptr; }

    // Simple destructor for cleanup (though not strictly needed for the crash to happen)
    // In a real crash scenario, we crash before this.
    void release() {
        if (buffer_ptr) delete[] buffer_ptr;
        buffer_ptr = nullptr;
    }
};
}

// Mock definitions
struct CompressedFormatInfo;

typedef void (*DecompressionFunction)(
    uint32_t width, uint32_t height, uint32_t depth,
    const uint8_t* input_data,
    uint32_t input_row_pitch,
    uint32_t input_depth_pitch,
    uint8_t* output_data,
    uint32_t output_row_pitch,
    uint32_t output_depth_pitch);

struct CompressedFormatInfo {
    int decompressed_format;
    int decompressed_type;
    DecompressionFunction decompression_function;
};

struct ContextState {
    void* api() const { return nullptr; }
};

struct GLES2Util {
    static uint32_t ComputeImageGroupSize(int format, int type) {
        // Mocking 4 bytes per pixel
        return 4;
    }
};

uint32_t GetCompressedFormatRowPitch(const CompressedFormatInfo& info, uint32_t width) {
    return width; // Mock value
}

uint32_t GetCompressedFormatDepthPitch(const CompressedFormatInfo& info, uint32_t width, uint32_t height) {
    return width * height; // Mock value
}

// The mock decompression function that actually writes out of bounds
void RealMockDecompressionFunction(
    uint32_t width, uint32_t height, uint32_t depth,
    const uint8_t* input_data,
    uint32_t input_row_pitch,
    uint32_t input_depth_pitch,
    uint8_t* output_data,
    uint32_t output_row_pitch,
    uint32_t output_depth_pitch) {

    std::cout << "[MockDecompression] Decompressing " << width << "x" << height << "x" << depth << std::endl;
    std::cout << "  Output Depth Pitch (step between layers): " << output_depth_pitch << std::endl;

    // Simulate writing to each layer
    for (uint32_t z = 0; z < depth; ++z) {
        uint32_t offset = z * output_depth_pitch;
        std::cout << "  Writing Layer " << z << " at offset " << offset << std::endl;

        // We just write one byte at the start of the layer to trigger the overflow
        // If z=0, offset=0. Buffer size is (width*height*4).
        // If z=1, offset=width*height*4. This is EXACTLY out of bounds.

        // Writing to the first byte of this layer
        // This is line 77 in the description PoC log
        output_data[offset] = 0xAA;
    }
}

// The vulnerable function
base::HeapArray<uint8_t> DecompressTextureData(const ContextState& state,
                                               const CompressedFormatInfo& info,
                                               uint32_t width,
                                               uint32_t height,
                                               uint32_t depth, // <--- Depth passed here
                                               int image_size,
                                               const void* input_data) {
  uint32_t output_pixel_size = GLES2Util::ComputeImageGroupSize(
      info.decompressed_format, info.decompressed_type);

  // [1] VULNERABILITY: Allocation size ignores 'depth'.
  // It allocates space for a single 2D slice (width * height).
  auto decompressed_data =
      base::HeapArray<uint8_t>::Uninit(output_pixel_size * width * height);

  // [2] The decompression function is passed 'depth'.
  // It writes (width * height * depth) pixels.
  info.decompression_function(
      width, height, depth, static_cast<const uint8_t*>(input_data),
      GetCompressedFormatRowPitch(info, width),
      GetCompressedFormatDepthPitch(info, width, height),
      decompressed_data.data(),
      output_pixel_size * width,          // outputRowPitch
      output_pixel_size * width * height  // outputDepthPitch
  );

  return decompressed_data;
}

int main() {
    std::cout << "Running ASAN PoC..." << std::endl;

    ContextState state;
    CompressedFormatInfo info;
    info.decompressed_format = 0;
    info.decompressed_type = 0;
    info.decompression_function = RealMockDecompressionFunction;

    // We use 4x4 image, depth 2.
    // Pixel size is 4 bytes.
    // 2D slice size = 4 * 4 * 4 = 64 bytes.
    // Allocation will be 64 bytes.
    // We need 128 bytes for 2 layers.
    uint32_t width = 4;
    uint32_t height = 4;
    uint32_t depth = 2;
    uint8_t dummy_input[100]; // Dummy input

    auto result = DecompressTextureData(state, info, width, height, depth, 0, dummy_input);
    result.release();

    return 0;
}
