from playwright.sync_api import sync_playwright
import time

def verify_dos():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()

        try:
            print("Navigating to DoS PoC...")
            page.goto("http://localhost:8080/WebXR_DoS.html")

            # Wait for the page to load and potentially freeze
            # If the main thread is frozen, page interactions might timeout
            print("Page loaded. Waiting for freeze effect...")

            # We can check if the console log appeared
            # page.on("console", lambda msg: print(f"CONSOLE: {msg.text}"))

            # Wait a bit to let the "busy loop" start
            time.sleep(2)

            print("Attempting to take screenshot (might be delayed if main thread is blocked)...")
            page.screenshot(path="verification/dos_verification.png")
            print("Screenshot taken.")

        except Exception as e:
            print(f"Error during verification: {e}")
        finally:
            browser.close()

if __name__ == "__main__":
    verify_dos()
