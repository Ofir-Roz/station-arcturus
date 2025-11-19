import time
from . import beacons
from . import constants


def register_socket_handlers(sio):
    """Register Socket.IO event handlers on the provided sio instance."""

    @sio.event
    async def connect(sid, environ):
        print(f"Client connected: {sid}")
        # Send current beacons when a client connects
        beacon_list = list(beacons.beacon_list_unity_dict())
        await sio.emit(constants.SOCKET_EVENT_BEACON_UPDATE, {"beacons": beacon_list, "time": time.time()}, to=sid)
        await sio.emit(constants.SOCKET_EVENT_CONNECTION_STATUS, {"status": "connected", "message": "Welcome to Station Arcturus Command Room"}, to=sid)

    @sio.event
    async def disconnect(sid):
        print(f"Client disconnected: {sid}")

    @sio.event
    async def ping(sid):
        await sio.emit(constants.SOCKET_EVENT_PONG, {"time": time.time()}, to=sid)

    @sio.event
    async def request_beacon_data(sid):
        beacon_list = list(beacons.beacon_list_unity_dict())
        await sio.emit(constants.SOCKET_EVENT_BEACON_UPDATE, {"beacons": beacon_list, "time": time.time()}, to=sid)