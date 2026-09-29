# Sends eight real chat requests together to make the assistant's capacity limit visible.
# This is a manual runtime experiment, not a test suite; it logs in through Gateway and prints responses without tokens.
import concurrent.futures
import json
import os
import threading
import time
import urllib.error
import urllib.request

BASE_URL = os.environ.get("EVENTHUB_GATEWAY", "http://localhost:5100")


def post(path, payload, token=None):
    """Send one HTTP request without retrying; keep bearer tokens out of printed evidence."""
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = "Bearer " + token
    request = urllib.request.Request(BASE_URL + path, json.dumps(payload).encode(), headers, method="POST")
    try:
        with urllib.request.urlopen(request, timeout=135) as response:
            return response.status, json.load(response)
    except urllib.error.HTTPError as error:
        return error.code, json.load(error)


def main():
    """Obtain a fresh demo login, release eight workers together, then print their actual outcomes."""
    status, login = post("/identity/auth/login", {"email": "attendee@demo.com", "password": "Demo@123"})
    if status != 200:
        raise SystemExit("Demo login failed: " + str(status))
    barrier = threading.Barrier(8)

    def send(number):
        """Start with the other workers so an empty seven-slot capacity is exceeded by one request."""
        barrier.wait()
        started = time.monotonic()
        status, body = post("/agent/chat", {"messages": [{"role": "user", "content": "Music events under ₹1000?"}]}, login["accessToken"])
        return {"request": number, "status": status, "seconds": round(time.monotonic() - started, 2), "body": body}

    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as workers:
        futures = [workers.submit(send, number) for number in range(1, 9)]
        for future in concurrent.futures.as_completed(futures):
            print(json.dumps(future.result(), ensure_ascii=False), flush=True)


if __name__ == "__main__":
    main()
