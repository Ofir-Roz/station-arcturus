# Station Arcturus - Backend

Real-time beacon tracking server built with FastAPI and python-socketio.

## Overview

The backend server manages beacon data for Station Arcturus, providing both REST API endpoints and real-time WebSocket updates. It generates, simulates, and broadcasts beacon positions and statuses to connected clients (including the Unity frontend).

## Architecture

- `app/` - modularized application package:
  - `main.py` - FastAPI app initialization, Socket.IO integration, CORS middleware, HTTP endpoints, and lifecycle events
  - `models.py` - Pydantic models for `Beacon` and `BeaconStatus` enums
  - `beacons.py` - beacon generation logic, simulation updates, and background updater task
  - `events.py` - Socket.IO event handlers for client connections and requests
- `run.py` - server entrypoint that launches the ASGI app via Uvicorn
- `static/test_client.html` - browser-based test client for WebSocket connectivity and beacon visualization

## API Endpoints

- `GET /` - health check, returns server status message
- `GET /status` - returns server status with beacon count and timestamp
- `GET /beacons` - returns all current beacons with their positions and statuses
- `GET /ui` - serves the test client HTML page

## WebSocket Events

- `beacon_update` - broadcast event with beacon data updates
- `connect` / `disconnect` - client connection events

## Setup & Installation

1. **Navigate to the backend directory:**

   ```bash
   cd backend
   ```

2. **Create and activate a virtual environment:**

   **Windows:**
   ```bash
   python -m venv .venv
   .venv\Scripts\activate
   ```

   **macOS/Linux:**
   ```bash
   python -m venv .venv
   source .venv/bin/activate
   ```

3. **Install dependencies:**

   ```bash
   pip install -r requirements.txt
   ```

## Running the Server

Start the development server:

```bash
python run.py
```

The server will start on `http://127.0.0.1:8000`

**Test the server:**

- Open `http://127.0.0.1:8000/ui` for the browser test client
- Access `http://127.0.0.1:8000/beacons` for beacon data via REST API
- Check `http://127.0.0.1:8000/status` for server status

## Using with Pre-Built Frontends

The backend is designed to work with the pre-built Unity executables located in the `Builds/` folder at the project root. Simply start the backend server as described above, then run the appropriate executable for your platform:

- **Windows**: Run `Builds/Windows/Station Arcturus Command Room.exe`
- **macOS**: Run `Builds/macOS.app`

The frontend will automatically connect to the backend server at `http://127.0.0.1:8000`.

## Development Notes

- CORS is configured to allow all origins for development - tighten this for production
- The beacon updater runs as a background asyncio task and broadcasts updates every few seconds
- Socket.IO runs in ASGI async mode for optimal performance
- All beacon coordinates use a flat coordinate system (x, altitude, z)
