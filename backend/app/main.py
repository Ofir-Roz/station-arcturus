from fastapi import FastAPI
from fastapi.staticfiles import StaticFiles
from fastapi.responses import FileResponse
from pathlib import Path
from fastapi.middleware.cors import CORSMiddleware
from contextlib import asynccontextmanager
import socketio
import asyncio
import time

from . import beacons
from . import events


@asynccontextmanager
async def lifespan(app: FastAPI):
    # Startup
    updater_task = asyncio.create_task(beacons.beacon_updater(sio))
    print("Station Arcturus Backend started!")
    
    yield
    
    # Shutdown
    updater_task.cancel()
    try:
        await updater_task
    except asyncio.CancelledError:
        pass
    print("Station Arcturus Backend shutting down!")


app = FastAPI(title="Station Arcturus Backend", lifespan=lifespan)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Socket.IO server
sio = socketio.AsyncServer(async_mode='asgi', cors_allowed_origins='*')
socket_app = socketio.ASGIApp(sio, app)

# Initialize beacons
beacons.beacons_data = beacons.generate_initial_beacons()

# Register socket event handlers
events.register_socket_handlers(sio)

# Serve a static test client
static_dir = Path(__file__).resolve().parent.parent / "static"
app.mount("/static", StaticFiles(directory=str(static_dir)), name="static")

@app.get("/ui")
async def ui():
    return FileResponse(static_dir / "test_client.html")


# HTTP endpoints
@app.get("/")
async def root():
    return {"message": "Station Arcturus Backend is running."}


@app.get("/status")
async def status():
    return {"status": "alive", "beacons_count": len(beacons.beacons_data), "time": time.time()}


@app.get("/beacons")
async def get_beacons():
    beacon_list = [b.to_unity_dict() for b in beacons.beacons_data.values()]
    return {"beacons": beacon_list, "count": len(beacon_list), "time": time.time()}
