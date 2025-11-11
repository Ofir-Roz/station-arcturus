from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
import socketio
import json
from typing import Dict, List
import asyncio
import random
import time
from pydantic import BaseModel
from enum import Enum

# Create FastAPI app
app = FastAPI(title="Station Arcturus Backend")

# Set up CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"], # In production, specify allowed origins
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Create Socket.IO server
sio = socketio.AsyncServer(
    async_mode='asgi',
    cors_allowed_origins='*' # allow all origins
)

# -----------------------------------------------------------------------------

#Define beacon status types
class BeaconStatus(str, Enum):
    ACTIVE = "active"
    INACTIVE = "inactive"
    ERROR = "error"


# Define how a beacon looks like
class Beacon(BaseModel): 
    id: str
    x: float
    y: float
    z: float
    status: BeaconStatus

    # Convert beacon to dict for Unity consumption
    def to_unity_dict(self):
        return {
            "id": self.id,
            "x": self.x,
            "y": self.y,
            "z": self.z,
            "status": self.status.value
        }
    

# Generate initial beacons
def generate_initial_beacons(count: int = 8) -> Dict[str, Beacon]:
    """ Generate random beacons on planet surface """
    beacons = {}

    for i in range(count):
        beacon_id = f"BEACON-{i+1:04d}" # e.g., BEACON-0001

        beacon = Beacon(
                id=beacon_id,
                x=round(random.uniform(-50, 50), 3),
                y=round(random.uniform(0, 20), 3),
                z=round(random.uniform(-50, 50), 3),
                status=random.choice(list(BeaconStatus))
            )

        beacons[beacon_id] = beacon
        print(f"Generated {beacon_id} at ({beacon.x}, {beacon.y}, {beacon.z}) with status {beacon.status}")
    
    return beacons

# make beacons dynamic
async def update_beacon_positions():
    """Simulates beacon movement (drift)"""

    for beacon_id, beacon in beacons_data.items():
        # small chance to move beacon
        if random.random() < 0.3: 
            # small random drift
            beacon.x += round(random.uniform(-2, 2), 3)
            beacon.y += round(random.uniform(-0.5, 0.5), 3)
            beacon.z += round(random.uniform(-2, 2), 3)

            # keep within bounds
            beacon.x = max(-50, min(50, beacon.x))
            beacon.y = max(0, min(20, beacon.y))
            beacon.z = max(-50, min(50, beacon.z))


async def update_beacons_statuses():
    """Simulates beacon status changes (equipment failure/recovery)"""
    for beacon_id, beacon in beacons_data.items():
        # small chance to change status
        if random.random() < 0.15: 
            old_status = beacon.status
            beacon.status = random.choice(list(BeaconStatus))

            if old_status != beacon.status:
                print(f"Beacon {beacon_id} changed status from {old_status} to {beacon.status}")


async def simulate_beacon_changes():
    """ randomly add or remove beacons occasionally """
    global beacons_data

    # small chance to add a new beacon
    if random.random() < 0.1 and len(beacons_data) < 15: # 10% chance, max 15 beacons
        new_id = f"BEACON-{random.randint(1000, 9999)}"

        if new_id not in beacons_data:
            beacons_data[new_id] = Beacon(
                id=new_id,
                x=round(random.uniform(-50, 50), 3),
                y=round(random.uniform(0, 20), 3),
                z=round(random.uniform(-50, 50), 3),
                status=random.choice(list(BeaconStatus))
            )
            print(f"New beacon appeared: {new_id}")

    # small chance to remove an existing beacon
    if random.random() < 0.1 and len(beacons_data) > 3: # 10% chance, min 3 beacons
        beacon_to_remove = random.choice(list(beacons_data.keys()))
        del beacons_data[beacon_to_remove]
        print(f"Beacon Lost: {beacon_to_remove}")


# Global variable to track if updater is running
updater_task = None

async def beacon_updater():
    """ Background task to update beacons every few seconds """
    print("Beacon updater started!")

    while True:
        try:
            await update_beacon_positions()
            await update_beacons_statuses()
            await simulate_beacon_changes()

            # Send updates to all connected clients via websockets
            if sio.manager.rooms:
                beacon_list = [beacon.to_unity_dict() for beacon in beacons_data.values()]
                await sio.emit('beacon_update', {
                    "beacons": beacon_list,
                    "time": time.time()
                })
            
            await asyncio.sleep(2) # update every 2 seconds

        except Exception as e:
            print(f"Error in beacon updater: {e}")
            await asyncio.sleep(2) # wait before retrying
            
# -----------------------------------------------------------------------------

# warp FastAPI app with Socket.IO
socket_app = socketio.ASGIApp(sio, app)

# Initialize beacons when server starts
beacons_data = generate_initial_beacons()


# Start the beacon updater task when the server starts
@app.on_event("startup")
async def startup_event():
    """ Start background tasks on server startup """
    global updater_task
    updater_task = asyncio.create_task(beacon_updater())
    print("Station Arcturus Backend started!")

@app.on_event("shutdown")
async def shutdown_event():
    """ Clean up background tasks on server shutdown """
    global updater_task
    if updater_task:
        updater_task.cancel()
    print("Station Arcturus Backend shutting down!")

# Websockets events
@sio.event
async def connect(sid, environ):
    """Called when a client connects"""
    print(f"Client connected: {sid}")

    # Send current beacons state to the newly connected client
    beacon_list = [beacon.to_unity_dict() for beacon in beacons_data.values()]
    await sio.emit('beacon_update', {
        "beacons": beacon_list,
        "time": time.time()
    }, to=sid)

    # Send a welcome message
    await sio.emit('connection_status', {
        "status": "connected",
        "message": "Welcome to Station Arcturus Command Room",
    }, to=sid)

@sio.event
async def disconnect(sid):
    """Called when a client disconnects"""
    print(f"Client disconnected: {sid}")

@sio.event
async def ping(sid):
    """Simple ping-pong to check connection"""
    await sio.emit('pong', {
        "time": time.time()
    }, to=sid)

@sio.event
async def request_beacon_data(sid):
    """Client can manually request latest beacon data"""
    beacon_list = [beacon.to_unity_dict() for beacon in beacons_data.values()]
    await sio.emit('beacon_update', {
        "beacons": beacon_list,
        "time": time.time()
    }, to=sid)

# -----------------------------------------------------------------------------

# basic test endpoint
@app.get("/")
async def root():
    return {"message": "Station Arcturus Backend is running."}

# Test endpoint to check if server is alive
@app.get("/status")
async def status():
    return {"status": "alive",
            "beacons_count": len(beacons_data),
            "time": time.time()
    }


@app.get("/beacons")
async def get_beacons():
    """ HTTP endpoint to get all beacons """
    beacon_list = [beacon.to_unity_dict() for beacon in beacons_data.values()]
    return {"beacons": beacon_list,
            "count": len(beacon_list),
            "time": time.time()
    }



if __name__ == "__main__":
    import uvicorn
    uvicorn.run(socket_app, host="0.0.0.0", port=8000)

