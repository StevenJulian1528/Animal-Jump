from playwright.sync_api import sync_playwright

def verify_xss():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()

        # Listen for dialogs (alerts)
        page.on("dialog", lambda dialog: print(f"ALERT: {dialog.message}"))

        try:
            # Navigate to the hosted PoC
            page.goto("http://localhost:8080/WebXR_XSS.html")

            # Wait a bit for the script to execute
            page.wait_for_timeout(5000)

            # Take a screenshot
            page.screenshot(path="verification/xss_verification.png")

        except Exception as e:
            print(f"Error: {e}")
        finally:
            browser.close()

if __name__ == "__main__":
    verify_xss()
