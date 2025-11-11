import time
from . import beacons


def register_socket_handlers(sio):
    """Register Socket.IO event handlers on the provided sio instance."""

    @sio.event
    async def connect(sid, environ):
        print(f"Client connected: {sid}")
        # Send current beacons when a client connects
        beacon_list = [b.to_unity_dict() for b in beacons.beacons_data.values()]
        await sio.emit('beacon_update', {"beacons": beacon_list, "time": time.time()}, to=sid)
        await sio.emit('connection_status', {"status": "connected", "message": "Welcome to Station Arcturus Command Room"}, to=sid)

    @sio.event
    async def disconnect(sid):
        print(f"Client disconnected: {sid}")

    @sio.event
    async def ping(sid):
        await sio.emit('pong', {"time": time.time()}, to=sid)

    @sio.event
    async def request_beacon_data(sid):
        beacon_list = [b.to_unity_dict() for b in beacons.beacons_data.values()]
        await sio.emit('beacon_update', {"beacons": beacon_list, "time": time.time()}, to=sid)
