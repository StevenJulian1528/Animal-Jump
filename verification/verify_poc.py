from playwright.sync_api import Page, expect, sync_playwright
import os

def verify_poc(page: Page):
    # Load the local HTML file
    cwd = os.getcwd()
    url = f"file://{cwd}/poc.html"
    print(f"Loading {url}")
    page.goto(url)

    # Wait for the logs to appear
    # We check if the 'Done' or 'Error' message appears in logs
    # or if the status updates.
    try:
        expect(page.locator("#log")).to_contain_text("Command executed", timeout=5000)
        print("Page loaded and command executed successfully (no immediate crash).")
    except Exception as e:
        print(f"Expectation failed: {e}")

    # Take a screenshot
    page.screenshot(path="verification/poc_screenshot.png")

if __name__ == "__main__":
    with sync_playwright() as p:
        # Launch chromium. Note: This is NOT an ASAN build.
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            verify_poc(page)
        except Exception as e:
            print(f"Browser crashed or error occurred: {e}")
        finally:
            browser.close()
