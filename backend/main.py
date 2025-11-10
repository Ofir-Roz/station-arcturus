from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
import socketio
import json
from typing import Dict, List
import asyncio
import random
import time

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

# warp FastAPI app with Socket.IO
socket_app = socketio.ASGIApp(sio, app)

# This will store our beacons
beacons_data = {}

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

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(socket_app, host="0.0.0.0", port=8000)

