#pragma once
#include <vector>
#include <string>
#include <cstring>
#include <memory>
#include <iostream>
#include <stdexcept>
#include <algorithm>
#include <climits>
#include <type_traits>

namespace folly {

class IOBuf {
public:
    std::vector<uint8_t> data_;

    static std::unique_ptr<IOBuf> create(size_t capacity) {
        auto buf = std::make_unique<IOBuf>();
        buf->data_.resize(capacity);
        return buf;
    }

    const uint8_t* data() const { return data_.data(); }
    uint8_t* writableData() { return data_.data(); }
    size_t length() const { return data_.size(); }
    size_t computeChainDataLength() const { return data_.size(); } // Simplified: no chain

    void append(size_t len) {
        // Mock: do nothing, assume data is written
    }

    void trimStart(size_t amount) {
         if (amount > data_.size()) amount = data_.size();
         data_.erase(data_.begin(), data_.begin() + amount);
    }
};

class IOBufQueue {
public:
    std::vector<uint8_t> buffer_;
    size_t virtualLength_ = 0; // Added for testing huge buffers without allocation

    IOBufQueue(bool cache = false) {}

    void append(std::unique_ptr<IOBuf> buf) {
        buffer_.insert(buffer_.end(), buf->data_.begin(), buf->data_.end());
    }

    // Allow test to lie about length
    void setVirtualLength(size_t len) {
        virtualLength_ = len;
    }

    size_t chainLength() const {
        return buffer_.size() + virtualLength_;
    }

    const uint8_t* front() const {
        return buffer_.data();
    }

    void trimStart(size_t amount) {
        if (amount <= buffer_.size()) {
            buffer_.erase(buffer_.begin(), buffer_.begin() + amount);
        } else {
            size_t remainder = amount - buffer_.size();
            buffer_.clear();
            if (remainder <= virtualLength_) {
                virtualLength_ -= remainder;
            } else {
                 throw std::underflow_error("trimStart underflow");
            }
        }
    }

    void trimStartAtMost(size_t amount) {
        // Simplified
        trimStart(std::min(amount, chainLength()));
    }

    std::unique_ptr<IOBuf> split(size_t n) {
        // HERE IS THE PAYLOAD: Checking if n is HUGE (negative int cast to size_t)
        // On 64-bit systems, size_t is uint64.
        // If n comes from (int)negative, it will be > 0xFFFFFFFF usually.
        // Let's check for > 4GB.
        if (n > 0xFFFFFFFF) {
            std::cout << "[CRITICAL] IOBufQueue::split called with HUGE size: " << n << std::endl;
            std::cout << "[SUCCESS] Integer Overflow vulnerability reproduced!" << std::endl;
            exit(0); // Exit successfully proving the bug
        }

        if (n > chainLength()) {
             throw std::underflow_error("split underflow");
        }

        // Mock split return
        auto ret = IOBuf::create(std::min((size_t)1024, n)); // Just return something
        return ret;
    }
};

template <class T, class S>
std::string to(S val) {
    if constexpr (std::is_same_v<S, std::string>) {
        return val;
    } else {
        return std::to_string(val);
    }
}

// Mock ExceptionWrapper
class exception_wrapper {
public:
    std::exception_ptr ptr;
    template <class E>
    exception_wrapper(E&& e) {}
};

template <class E, typename... Args>
exception_wrapper make_exception_wrapper(Args&&... args) {
    return exception_wrapper(E(std::forward<Args>(args)...));
}

namespace io {
class Cursor {
    const uint8_t* ptr_;
public:
    Cursor(const uint8_t* ptr) : ptr_(ptr) {} // simplified, takes raw ptr

    void skip(size_t n) { ptr_ += n; }

    template <typename T>
    T readBE() {
        T val = 0;
        uint8_t* dest = (uint8_t*)&val;
        for (size_t i = 0; i < sizeof(T); ++i) {
            dest[sizeof(T) - 1 - i] = *ptr_++;
        }
        return val;
    }

    template <typename T>
    T readLE() {
        T val = 0;
        std::memcpy(&val, ptr_, sizeof(T));
        ptr_ += sizeof(T);
        return val;
    }
};
} // namespace io

} // namespace folly

namespace wangle {
    // Stub HandlerContext
    template <typename In, typename Out>
    class HandlerContext {
    public:
        virtual void fireRead(Out msg) = 0;
        virtual void fireReadException(folly::exception_wrapper e) = 0;
        virtual void fireTransportActive() {}
        virtual void fireTransportInactive() {}
    };

    template <typename In, typename Out>
    class InboundHandler {
    public:
        using Context = HandlerContext<In, Out>;
        virtual void read(Context*, In) = 0;
        virtual void transportActive(Context*) {}
        virtual void transportInactive(Context*) {}
    };
}

// Define CHECK
#define CHECK(condition) if(!(condition)) { std::cerr << "CHECK failed: " << #condition << std::endl; std::terminate(); }
