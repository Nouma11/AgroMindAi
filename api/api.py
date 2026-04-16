import os
import math
from typing import Optional
import joblib
import pandas as pd
import requests
import google.generativeai as genai
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from dotenv import load_dotenv

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIR = os.path.join(BASE_DIR, "..")

load_dotenv(os.path.join(PROJECT_DIR, ".env"))

app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=[
        "http://localhost:5173",
        "http://localhost:3000",
        "http://127.0.0.1:5173",
        "http://127.0.0.1:3000",
    ],
    # localhost / 127.0.0.1 / typical LAN IPs when opening the app as http://192.168.x.x:5173
    allow_origin_regex=r"http://(localhost|127\.0\.0\.1|192\.168\.\d{1,3}\.\d{1,3}|10\.\d{1,3}\.\d{1,3}\.\d{1,3})(:\d+)?$",
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

try:
    crop_model = joblib.load(os.path.join(PROJECT_DIR, "models", "crop_model.pkl"))
    fert_model = joblib.load(os.path.join(PROJECT_DIR, "models", "fertilizer_model.pkl"))
    columns = joblib.load(os.path.join(PROJECT_DIR, "models", "columns.pkl"))
except FileNotFoundError as e:
    print(f"Error loading model: {e}")
    crop_model = None
    fert_model = None
    columns = None


class PredictionInput(BaseModel):
    Temparature: float
    Humidity: float
    Moisture: float
    Soil_Type: str
    Nitrogen: float
    Potassium: float
    Phosphorous: float


class ExplainInput(BaseModel):
    crop: str
    fertilizer: str
    location: str
    soil_type: str
    temperature: float
    humidity: float
    nitrogen: float
    potassium: float
    phosphorous: float
    moisture: Optional[float] = None
    farmer_question: str = ""


LOCATION_COORDS = {
    "Tunis": (36.8065, 10.1815),
    "Sousse": (35.8256, 10.6369),
    "Sfax": (34.7406, 10.7603),
    "Nabeul": (36.4561, 10.7376),
    "Mednine": (33.3549, 10.5055),
    "Gabes": (33.8815, 10.0982),
}


def find_nearest_location(latitude: float, longitude: float) -> str:
    nearest = None
    min_dist = float("inf")
    for name, (lat, lon) in LOCATION_COORDS.items():
        dist = math.sqrt((latitude - lat) ** 2 + (longitude - lon) ** 2)
        if dist < min_dist:
            min_dist = dist
            nearest = name
    return nearest


def simplify_llm_reason(reason: str) -> str:
    reason_lower = reason.lower()
    if "429" in reason_lower or "quota" in reason_lower:
        return "Gemini quota is currently exceeded"
    if "api key" in reason_lower:
        return "Gemini API key is missing or invalid"
    return "the LLM service is currently unavailable"


def build_fallback_explanation(data: ExplainInput, reason: str = "") -> str:
    nutrient_status = []
    nutrient_status.append("strong nitrogen support" if data.nitrogen >= 50 else "moderate nitrogen availability")
    nutrient_status.append("good phosphorus availability" if data.phosphorous >= 40 else "limited phosphorus levels")
    nutrient_status.append("good potassium support" if data.potassium >= 40 else "limited potassium support")

    moisture_text = ""
    if data.moisture is not None:
        moisture_text = f" Soil moisture is around {data.moisture}% which is useful for irrigation planning."

    question_text = f" In response to the farmer's question, \"{data.farmer_question}\", " if data.farmer_question else " "

    fallback = (
        f"{data.crop} is recommended for {data.location} because the current conditions show {data.soil_type.lower()} soil, "
        f"a temperature of {data.temperature}°C, and humidity near {data.humidity}%, which are compatible with stable crop development."
        f"{question_text}{data.fertilizer} is suggested to support the crop because the soil profile indicates {', '.join(nutrient_status)}."
        f"{moisture_text} For best results, monitor irrigation closely, apply fertilizer in measured doses, and confirm the plan with a local soil test before planting."
    )

    if reason:
        fallback += f" Note: this explanation was generated using a local fallback because {reason}."

    return fallback


@app.get("/features")
def get_features(latitude: float, longitude: float):
    try:
        location = find_nearest_location(latitude, longitude)

        # Get weather data from OpenWeatherMap
        weather_key = os.getenv("OPENWEATHER_API_KEY")
        if not weather_key:
            return {"error": "OPENWEATHER_API_KEY not set"}

        weather_url = f"https://api.openweathermap.org/data/2.5/weather?lat={latitude}&lon={longitude}&appid={weather_key}&units=metric"
        weather_resp = requests.get(weather_url)
        weather_data = weather_resp.json()

        temperature = weather_data.get("main", {}).get("temp", 0)
        humidity = weather_data.get("main", {}).get("humidity", 0)

        # Get soil data from CSV
        soil_df = pd.read_csv(os.path.join(PROJECT_DIR, "data", "soil_dataset.csv"))
        location_data = soil_df[soil_df["Location"] == location]

        if location_data.empty:
            return {"error": f"No soil data found for {location}"}

        avg_data = location_data.mean(numeric_only=True)

        return {
            "location": location,
            "Temparature": round(temperature, 2),
            "Humidity": round(humidity, 2),
            "Moisture": round(avg_data.get("Moisture", 0), 2),
            "Soil_Type": location_data["Soil_Type"].mode().iloc[0] if "Soil_Type" in location_data.columns else "Unknown",
            "Nitrogen": round(avg_data.get("Nitrogen", 0), 2),
            "Potassium": round(avg_data.get("Potassium", 0), 2),
            "Phosphorous": round(avg_data.get("Phosphorous", 0), 2),
        }

    except requests.exceptions.RequestException as e:
        return {"error": f"Weather API error: {str(e)}"}
    except Exception as e:
        return {"error": f"Failed to get features: {str(e)}"}


@app.post("/predict")
def predict(data: PredictionInput):
    try:
        if crop_model is None or fert_model is None or columns is None:
            return {"error": "Model not loaded. Run train.py first."}

        df = pd.DataFrame([data.dict()])

        # Feature engineering (must match train.py)
        df["N_K_ratio"] = df["Nitrogen"] / (df["Potassium"] + 1)
        df["P_N_ratio"] = df["Phosphorous"] / (df["Nitrogen"] + 1)
        df["Temp_Humidity"] = df["Temparature"] * df["Humidity"]

        df = pd.get_dummies(df)
        df = df.reindex(columns=columns, fill_value=0)

        crop_pred = crop_model.predict(df)
        fert_pred = fert_model.predict(df)

        return {
            "recommended_crop": str(crop_pred[0]),
            "recommended_fertilizer": str(fert_pred[0]),
        }
    except Exception as e:
        return {"error": f"Prediction failed: {str(e)}"}
def ask_ollama(prompt: str) -> str:
    try:
        response = requests.post(
            "http://localhost:11434/api/generate",
            json={
                "model": "llama3",  # or mistral, phi3, etc.
                "prompt": prompt,
                "stream": False
            },
            timeout=60
        )

        if response.status_code != 200:
            print("Ollama error:", response.text)
            return ""

        data = response.json()
        return data.get("response", "").strip()

    except Exception as e:
        print("Ollama exception:", str(e))
        return ""
@app.post("/explain")
def explain(data: ExplainInput):
    try:
        farmer_question = data.farmer_question.strip() or \
            "What crop and fertilizer are best for my land and why?"

        moisture_line = (
            f"- Moisture: {data.moisture}%\n"
            if data.moisture is not None else ""
        )

        prompt = f"""You are an agricultural advisor helping a farmer in {data.location}.

Farmer question:
{farmer_question}

Field conditions:
- Soil type: {data.soil_type}
- Temperature: {data.temperature}°C
- Humidity: {data.humidity}%
{moisture_line}- Nitrogen: {data.nitrogen} mg/kg
- Potassium: {data.potassium} mg/kg
- Phosphorous: {data.phosphorous} mg/kg

ML recommendation:
- Crop: {data.crop}
- Fertilizer: {data.fertilizer}

Write a clear, practical explanation for the farmer in 5-7 sentences.
Explain:
- why the crop fits these conditions
- why the fertilizer is suitable
- give one practical field tip

Avoid repeating the input.
Use simple farmer-friendly language.
"""

        explanation_text = ask_ollama(prompt)

        if not explanation_text:
            explanation_text = build_fallback_explanation(
                data,
                "Ollama returned empty response"
            )
            source = "fallback"
        else:
            source = "ollama"

        return {
            "crop": data.crop,
            "fertilizer": data.fertilizer,
            "explanation": explanation_text,
            "source": source,
        }

    except Exception as e:
        return {
            "crop": data.crop,
            "fertilizer": data.fertilizer,
            "explanation": build_fallback_explanation(data, str(e)),
            "source": "fallback",
        }

        # ─────────────────────────────────────────────────────────────
# ADD THESE TWO NEW ENDPOINTS TO YOUR EXISTING api/api.py FILE
# Place them after the /explain endpoint
# ─────────────────────────────────────────────────────────────

# 1. New request model (add with the other BaseModel classes)
class ClassifyInput(BaseModel):
    question: str

class AskFarmerInput(BaseModel):
    question: str
    latitude: Optional[float] = None
    longitude: Optional[float] = None


# ─────────────────────────────────────────────────────────────
# AGRICULTURE KEYWORDS for fast local classification
# ─────────────────────────────────────────────────────────────
AGRICULTURE_KEYWORDS = [
    # crops & plants
    "crop", "plant", "seed", "harvest", "yield", "grow", "cultivat",
    "wheat", "rice", "corn", "maize", "cotton", "sugarcane", "barley",
    "millet", "pulses", "vegetable", "fruit", "spice", "oil seed",
    # soil & nutrients
    "soil", "nitrogen", "phosphor", "potassium", "fertiliz", "npk",
    "compost", "manure", "organic", "pH", "loam", "clay", "sandy",
    # farming activities
    "irrigat", "water", "moisture", "farm", "field", "land", "agriculture",
    "pest", "disease", "weed", "spray", "herbicide", "pesticide", "fungicide",
    "plant", "sow", "till", "plow", "harvest", "season", "rotation",
    # weather & environment
    "temperature", "humidity", "rain", "drought", "flood", "climate",
    "weather", "frost", "sun", "shade",
    # livestock (farmers ask about animals too)
    "livestock", "cattle", "sheep", "goat", "poultry", "chicken",
    "cow", "milk", "breed", "feed",
    # economics
    "market", "price", "sell", "buy", "profit", "cost", "subsidy",
    # tools
    "tractor", "machine", "equipment", "tool", "drone",
]

NON_AGRICULTURE_TOPICS = [
    "politic", "sport", "football", "music", "movie", "film",
    "celebrity", "fashion", "cook", "recipe", "travel", "hotel",
    "war", "religion", "love", "relationship", "math problem",
    "history lesson", "coding", "programming", "software",
]


def is_agriculture_question(question: str) -> bool:
    """
    Fast keyword-based check before calling Gemini.
    Returns True if the question is likely agriculture-related.
    """
    q = question.lower()

    # Explicit non-agriculture triggers
    for kw in NON_AGRICULTURE_TOPICS:
        if kw in q:
            return False

    # Agriculture triggers
    for kw in AGRICULTURE_KEYWORDS:
        if kw in q:
            return True

    # Neutral / ambiguous — let Gemini handle it as farming advice
    return True


# ─────────────────────────────────────────────────────────────
# ENDPOINT 1: /classify
# ─────────────────────────────────────────────────────────────
@app.post("/classify")
def classify_question(data: ClassifyInput):
    q = data.question.strip()

    if not q:
        return {
            "is_agriculture": False,
            "reason": "empty_question"
        }

    # 1️⃣ Fast keyword check (VERY IMPORTANT for performance)
    fast_result = is_agriculture_question(q)

    # If clearly NOT agriculture → return immediately
    if not fast_result:
        return {
            "is_agriculture": False,
            "reason": "keyword_match"
        }

    # 2️⃣ Use Ollama for better classification
    try:
        prompt = f"""You are a classifier for a smart farming assistant.

Question:
"{q}"

Task:
Decide if this is related to agriculture.

Rules:
- YES → agriculture, farming, crops, soil, livestock, irrigation, fertilizer, weather for farming
- NO → sports, politics, coding, movies, general topics

Respond with ONLY one word:
YES or NO
"""

        response = ask_ollama(prompt)
        answer = response.strip().upper()

        is_agri = answer.startswith("Y")

        return {
            "is_agriculture": is_agri,
            "reason": "ollama_classifier"
        }

    except Exception:
        # fallback if Ollama fails
        return {
            "is_agriculture": fast_result,
            "reason": "ollama_fallback"
        }


# ─────────────────────────────────────────────────────────────
# ENDPOINT 2: /ask_farmer
# Answers general farming questions that don't need the ML model
# ─────────────────────────────────────────────────────────────
@app.post("/ask_farmer")
def ask_farmer(data: AskFarmerInput):
    q = data.question.strip()

    if not q:
        return {
            "answer": "Please provide a question.",
            "source": "error",
        }

    # Location context
    location_line = ""
    if data.latitude is not None and data.longitude is not None:
        nearest = find_nearest_location(data.latitude, data.longitude)
        location_line = f"The farmer is located near {nearest}, Tunisia.\n"

    try:
        prompt = f"""You are an expert agricultural advisor helping farmers in Tunisia and North Africa.

{location_line}
A farmer asks:
"{q}"

Rules:
- Only answer agriculture-related questions
- If NOT agriculture → say:
  "I am a farming assistant and can only help with agricultural questions."
- Answer in 4-6 sentences
- Be practical and simple
- Give real tips (watering, fertilizer, disease control, etc.)
"""

        answer_text = ask_ollama(prompt)

        if not answer_text:
            answer_text = (
                "I'm sorry, I couldn't generate an answer right now. "
                "Please try again."
            )
            source = "fallback"
        else:
            source = "ollama"

        return {
            "answer": answer_text,
            "source": source
        }

    except Exception as e:
        return {
            "answer": f"Error: {str(e)}",
            "source": "fallback",
        }
