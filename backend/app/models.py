from pydantic import BaseModel
from enum import Enum


class BeaconStatus(str, Enum):
    ACTIVE = "active"
    DAMAGED = "damaged"
    OFFLINE = "offline"


class Beacon(BaseModel):
    id: str
    x: float          # Flat X coordinate
    altitude: float   # Height above planet surface (renamed from 'y')
    z: float          # Flat Z coordinate
    status: BeaconStatus

    def to_unity_dict(self):
        return {
            "id": self.id,
            "x": self.x,
            "y": self.altitude,  # Send as 'y' for Unity compatibility
            "z": self.z,
            "status": self.status.value,
        }
