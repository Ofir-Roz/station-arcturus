import asyncio
import random
import time
from typing import Dict, Iterable

from .models import Beacon, BeaconStatus
from . import constants

# Global in-memory store for beacons. This will be initialized by the app.
# todo: In future, consider persisting to a database.
beacons_data: Dict[str, Beacon] = {}


def generate_initial_beacons(count: int = constants.INITIAL_BEACON_COUNT) -> Dict[str, Beacon]:
    """Generate an initial set of beacons with randomized positions & status."""
    beacons: Dict[str, Beacon] = {}
    for i in range(count):
        beacon_id = f"{constants.BEACON_ID_PREFIX}{i+1:04d}"
        beacon = Beacon(
            id=beacon_id,
            x=round(random.uniform(constants.BEACON_X_MIN, constants.BEACON_X_MAX), constants.BEACON_ROUND_PLACES),
            altitude=round(random.uniform(constants.BEACON_ALTITUDE_MIN, constants.BEACON_ALTITUDE_MAX), constants.BEACON_ROUND_PLACES),
            z=round(random.uniform(constants.BEACON_Z_MIN, constants.BEACON_Z_MAX), constants.BEACON_ROUND_PLACES),
            status=random.choice(list(BeaconStatus)),
        )
        beacons[beacon_id] = beacon
        print(
            f"Generated {beacon_id} at ({beacon.x}, {beacon.altitude}, {beacon.z}) with status {beacon.status}"
        )
    return beacons


def _clamp_position(beacon: Beacon) -> None:
    beacon.x = max(constants.BEACON_X_MIN, min(constants.BEACON_X_MAX, beacon.x))
    beacon.altitude = max(constants.BEACON_ALTITUDE_MIN, min(constants.BEACON_ALTITUDE_MAX, beacon.altitude))
    beacon.z = max(constants.BEACON_Z_MIN, min(constants.BEACON_Z_MAX, beacon.z))


async def update_beacon_positions() -> None:
    """Probabilistically update positions of existing beacons."""
    for beacon in beacons_data.values():
        if random.random() < constants.POSITION_UPDATE_PROBABILITY:
            beacon.x += round(
                random.uniform(constants.POSITION_DELTA_XZ_MIN, constants.POSITION_DELTA_XZ_MAX),
                constants.BEACON_ROUND_PLACES,
            )
            beacon.altitude += round(
                random.uniform(constants.ALTITUDE_DELTA_MIN, constants.ALTITUDE_DELTA_MAX),
                constants.BEACON_ROUND_PLACES,
            )
            beacon.z += round(
                random.uniform(constants.POSITION_DELTA_XZ_MIN, constants.POSITION_DELTA_XZ_MAX),
                constants.BEACON_ROUND_PLACES,
            )
            _clamp_position(beacon)


async def update_beacons_statuses() -> None:
    """Randomly change beacon statuses"""
    for beacon_id, beacon in beacons_data.items():
        if random.random() < constants.STATUS_CHANGE_PROBABILITY:
            old_status = beacon.status
            beacon.status = random.choice(list(BeaconStatus))
            if old_status != beacon.status:
                print(
                    f"Beacon {beacon_id} changed status from {old_status} to {beacon.status}"
                )


async def simulate_beacon_changes() -> None:
    """Possibly add or remove beacons based on probability thresholds."""
    global beacons_data
    # Addition
    if (
        random.random() < constants.NEW_BEACON_PROBABILITY
        and len(beacons_data) < constants.MAX_BEACONS
    ):
        new_id = f"{constants.BEACON_ID_PREFIX}{random.randint(constants.NEW_BEACON_ID_MIN, constants.NEW_BEACON_ID_MAX)}"
        if new_id not in beacons_data:
            beacons_data[new_id] = Beacon(
                id=new_id,
                x=round(random.uniform(constants.BEACON_X_MIN, constants.BEACON_X_MAX), constants.BEACON_ROUND_PLACES),
                altitude=round(
                    random.uniform(constants.BEACON_ALTITUDE_MIN, constants.BEACON_ALTITUDE_MAX),
                    constants.BEACON_ROUND_PLACES,
                ),
                z=round(random.uniform(constants.BEACON_Z_MIN, constants.BEACON_Z_MAX), constants.BEACON_ROUND_PLACES),
                status=random.choice(list(BeaconStatus)),
            )
            print(f"New beacon appeared: {new_id}")
    # Removal
    if (
        random.random() < constants.REMOVE_BEACON_PROBABILITY
        and len(beacons_data) > constants.MIN_BEACONS_FOR_REMOVAL
    ):
        beacon_to_remove = random.choice(list(beacons_data.keys()))
        del beacons_data[beacon_to_remove]
        print(f"Beacon Lost: {beacon_to_remove}")


def beacon_list_unity_dict() -> Iterable[dict]:
    """Helper returning an iterable of Unity-ready beacon dictionaries."""
    return (b.to_unity_dict() for b in beacons_data.values())


async def beacon_updater(sio) -> None:
    """Background task that updates beacons and emits updates via Socket.IO (sio)."""
    print("Beacon updater started!")
    while True:
        try:
            await update_beacon_positions()
            await update_beacons_statuses()
            await simulate_beacon_changes()

            # Emit current beacons to connected clients
            if sio.manager.rooms:
                beacon_list = list(beacon_list_unity_dict())
                await sio.emit(
                    constants.SOCKET_EVENT_BEACON_UPDATE,
                    {"beacons": beacon_list, "time": time.time()},
                )

            await asyncio.sleep(constants.UPDATE_LOOP_SLEEP_SECONDS)
        except Exception as e:
            print(f"Error in beacon updater: {e}")
            await asyncio.sleep(constants.UPDATE_LOOP_SLEEP_SECONDS)