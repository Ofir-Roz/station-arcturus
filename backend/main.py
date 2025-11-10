from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
import socketio
import json
from typing import Dict, List
import asyncio
import random
import time