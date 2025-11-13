import asyncio
import random
import math
import time
from typing import Dict

from .models import Beacon, BeaconStatus

# Global in-memory store for beacons. This will be initialized by the app.
beacons_data: Dict[str, Beacon] = {}


def generate_initial_beacons(count: int = 8) -> Dict[str, Beacon]:
    beacons = {}
    for i in range(count):
        beacon_id = f"BEACON-{i+1:04d}"
        beacon = Beacon(
            id=beacon_id,
            x=round(random.uniform(-100, 100), 3),  # Wider range for full sphere coverage
            altitude=round(random.uniform(0, 8), 3),  # Altitude above surface
            z=round(random.uniform(-100, 100), 3),  # Wider range
            status=random.choice(list(BeaconStatus)),
        )
        beacons[beacon_id] = beacon
        print(f"Generated {beacon_id} at ({beacon.x}, {beacon.altitude}, {beacon.z}) with status {beacon.status}")
    return beacons


async def update_beacon_positions():
    for beacon_id, beacon in beacons_data.items():
        if random.random() < 0.7:
            beacon.x += round(random.uniform(-2.5, 2.5), 3)
            beacon.altitude += round(random.uniform(-0.5, 0.5), 3)
            beacon.z += round(random.uniform(-2.5, 2.5), 3)
            beacon.x = max(-100, min(100, beacon.x))
            beacon.altitude = max(0, min(10, beacon.altitude))
            beacon.z = max(-100, min(100, beacon.z))


async def update_beacons_statuses():
    """Randomly change beacon statuses"""
    for beacon_id, beacon in beacons_data.items():
        if random.random() < 0.15:
            old_status = beacon.status
            beacon.status = random.choice(list(BeaconStatus))
            if old_status != beacon.status:
                print(f"Beacon {beacon_id} changed status from {old_status} to {beacon.status}")


async def simulate_beacon_changes():
    global beacons_data
    if random.random() < 0.1 and len(beacons_data) < 24:
        new_id = f"BEACON-{random.randint(1000, 9999)}"
        if new_id not in beacons_data:
            beacons_data[new_id] = Beacon(
                id=new_id,
                x=round(random.uniform(-100, 100), 3),
                altitude=round(random.uniform(0, 8), 3),
                z=round(random.uniform(-100, 100), 3),
                status=random.choice(list(BeaconStatus)),
            )
            print(f"New beacon appeared: {new_id}")
    if random.random() < 0.1 and len(beacons_data) > 6:
        beacon_to_remove = random.choice(list(beacons_data.keys()))
        del beacons_data[beacon_to_remove]
        print(f"Beacon Lost: {beacon_to_remove}")


async def beacon_updater(sio):
    """Background task that updates beacons and emits updates via Socket.IO (sio)."""
    print("Beacon updater started!")
    while True:
        try:
            await update_beacon_positions()
            await update_beacons_statuses()
            await simulate_beacon_changes()

            # Emit current beacons to connected clients
            if sio.manager.rooms:
                beacon_list = [b.to_unity_dict() for b in beacons_data.values()]
                await sio.emit("beacon_update", {"beacons": beacon_list, "time": time.time()})

            await asyncio.sleep(2)
        except Exception as e:
            print(f"Error in beacon updater: {e}")
            await asyncio.sleep(2)
