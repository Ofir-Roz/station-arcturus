"""Centralized constants for beacon simulation and socket event names.

Adjust values here (or later via an environment-driven settings layer) instead of scattering
magic numbers throughout the codebase.
"""
# Beacon identity
BEACON_ID_PREFIX = "BEACON-"
INITIAL_BEACON_COUNT = 8
NEW_BEACON_ID_MIN = 1000
NEW_BEACON_ID_MAX = 9999

# Spatial bounds
BEACON_X_MIN = -100
BEACON_X_MAX = 100
BEACON_Z_MIN = -100
BEACON_Z_MAX = 100
BEACON_ALTITUDE_MIN = 0
BEACON_ALTITUDE_MAX = 3
BEACON_ROUND_PLACES = 3

# Position update behavior
POSITION_UPDATE_PROBABILITY = 0.5
POSITION_DELTA_XZ_MIN = -20
POSITION_DELTA_XZ_MAX = 20
ALTITUDE_DELTA_MIN = -0.05
ALTITUDE_DELTA_MAX = 0.05

# Status change behavior
STATUS_CHANGE_PROBABILITY = 0.15

# Dynamic beacon population adjustments
NEW_BEACON_PROBABILITY = 0.1
REMOVE_BEACON_PROBABILITY = 0.1
MAX_BEACONS = 24
MIN_BEACONS_FOR_REMOVAL = 6

# Loop timing
UPDATE_LOOP_SLEEP_SECONDS = 2

# Socket event names
SOCKET_EVENT_BEACON_UPDATE = "beacon_update"
SOCKET_EVENT_CONNECTION_STATUS = "connection_status"
SOCKET_EVENT_PONG = "pong"

