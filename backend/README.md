# Station Arcturus - Backend

This folder contains the backend server for Station Arcturus built with FastAPI and python-socketio.

Structure

- `app/` - package containing modularized application code:
  - `main.py` - FastAPI app, Socket.IO wiring, startup/shutdown events and HTTP endpoints.
  - `models.py` - Pydantic models for Beacon and BeaconStatus.
  - `beacons.py` - Beacon generation, simulation, and background updater (non-socket logic).
  - `events.py` - Socket.IO event handlers registered on startup.
- `run.py` - simple wrapper to run the ASGI app via Uvicorn (entrypoint).
- `static/test_client.html` - test-page that connects to websocket & displays beacon data.

Running the backend

1. Activate the virtual environment (if any).
2. Install requirements:

```
pip install -r requirements.txt
```

3. Start the server:

```
python run.py
```

4. Open the test client in a browser at: `http://127.0.0.1:8000/ui`

Notes

- The app sends `beacon_update` events via Socket.IO. The test client is a simple web interface for quick checks.
- For production deployment, consider tightening CORS policies and using a process manager.
