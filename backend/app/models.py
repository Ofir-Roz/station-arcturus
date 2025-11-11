from pydantic import BaseModel
from enum import Enum


class BeaconStatus(str, Enum):
    ACTIVE = "active"
    INACTIVE = "inactive"
    ERROR = "error"


class Beacon(BaseModel):
    id: str
    x: float
    y: float
    z: float
    status: BeaconStatus

    def to_unity_dict(self):
        return {
            "id": self.id,
            "x": self.x,
            "y": self.y,
            "z": self.z,
            "status": self.status.value,
        }
