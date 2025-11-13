# Station Arcturus

A real-time beacon tracking system with a Unity 3D visualization frontend and a FastAPI backend server.

## Overview

Station Arcturus is a space station command center simulation that tracks and visualizes beacon positions in real-time. The system consists of:

- **Unity Frontend**: 3D visualization interface built with Unity 6000.2.10f1
- **Python Backend**: Real-time data server using FastAPI and Socket.IO

Beacons are tracked with x, altitude, and z coordinates, and can have different statuses (active, damaged, offline). The backend generates simulated beacon data and broadcasts updates to connected clients via WebSocket.

## Project Structure

```
station-arcturus/
├── Builds/                           # Pre-built executables (recommended)
│   ├── Windows/
│   │   └── Station Arcturus Command Room.exe
│   └── macOS.app/                    # macOS application bundle
├── frontend/
│   └── unity/
│       └── StationArcturusCommand/   # Unity 6000.2.10f1 project
│           ├── Assets/
│           ├── ProjectSettings/
│           └── ...
├── backend/                          # FastAPI + Socket.IO server
│   ├── app/
│   │   ├── main.py
│   │   ├── models.py
│   │   ├── beacons.py
│   │   └── events.py
│   ├── static/
│   ├── requirements.txt
│   └── run.py
└── README.md
```

## Quick Start

### Prerequisites

- **Python 3.8+** for the backend server
- **Unity 6000.2.10f1** (optional - only needed if you want to modify the Unity project)

### Running the Application (Recommended)

The easiest way to run Station Arcturus is using the pre-built executables.

#### 1. Start the Backend Server

```bash
# Navigate to the backend directory
cd station-arcturus/backend

# Create and activate a virtual environment (first time only)
python -m venv .venv
.venv\Scripts\activate  # On Windows
# source .venv/bin/activate  # On macOS/Linux

# Install dependencies (first time only)
pip install -r requirements.txt

# Start the server
python run.py
```

The server will start on `http://127.0.0.1:8000`. (may be localhost as well).

**Test the backend:**

- Open `http://127.0.0.1:8000/ui` for the browser test client
- Check `http://127.0.0.1:8000/status` for server status
- Access `http://127.0.0.1:8000/beacons` for beacon data

#### 2. Run the Pre-Built Frontend

**Windows:**

1. Navigate to `Builds/Windows/`
2. Double-click `Station Arcturus Command Room.exe`
3. The application will connect to the backend server automatically

**macOS:**

1. Navigate to `Builds/`
2. Double-click `macOS.app`
3. If you see a security warning, go to System Preferences > Security & Privacy and click "Open Anyway"
4. The application will connect to the backend server automatically

The 3D visualization will launch and connect to the backend server, displaying beacon positions in real-time.

### Running from Unity Editor (Development Only)

If you need to modify the Unity project:

1. Open Unity Hub
2. Add the project by selecting `frontend/unity/StationArcturusCommand`
3. Open the project with Unity 6000.2.10f1
4. Press Play in the Unity Editor

The Unity client will connect to the backend server and display beacon positions in 3D.

## System Architecture

### Backend Server

The backend provides:

- **REST API** for beacon data retrieval
- **WebSocket (Socket.IO)** for real-time beacon updates
- **Beacon simulation** with automatic position and status changes
- **Test client** for quick debugging without Unity

### Unity Frontend

The Unity client features:

- Real-time 3D visualization of beacon positions
- WebSocket connection to the backend server
- Dynamic beacon status rendering (active, damaged, offline)
- Interactive camera controls
- Planetary coordinate system visualization

## Development

### Backend Development

See [backend/README.md](backend/README.md) for detailed backend documentation.

Key technologies:

- FastAPI for REST endpoints
- python-socketio for real-time communication
- Uvicorn ASGI server
- Pydantic for data validation

### Unity Development

The Unity project is located at `frontend/unity/StationArcturusCommand/`

Key features:

- Beacon visualization with particle effects
- Orbital camera controller
- Real-time data synchronization
- Planetary coordinate mapping

## Configuration

### Backend Configuration

The backend runs on port 8000 by default. CORS is configured to allow all origins for development.

### Unity Configuration

The pre-built executables are configured to connect to `http://127.0.0.1:8000` by default. If you need to change the server URL, you'll need to modify the Unity project and rebuild.

## Troubleshooting

### Windows

- **Application won't start**: Make sure the backend server is running first
- **Firewall warnings**: Allow the application through Windows Firewall if prompted
- **Missing DLL errors**: Ensure all files in the `Builds/Windows/` folder remain together

### macOS

- **"Cannot be opened because it is from an unidentified developer"**: Right-click the app, select Open, then click Open in the dialog
- **Security warning persists**: Go to System Preferences > Security & Privacy > General, and click "Open Anyway"
- **Application won't start**: Verify the backend server is running on `http://127.0.0.1:8000`

### Backend

- **Port already in use**: Another application may be using port 8000. Stop it or modify the port in `backend/run.py`
- **Module not found errors**: Ensure you've activated the virtual environment and installed all dependencies

## License

_Add your license information here_

## Contributing

_Add contributing guidelines here_
