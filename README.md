# Agriculture ML - Crop Recommendation System with LLM Explanations

AI-powered crop recommendation system combining machine learning predictions with real-time weather data and LLM-generated explanations. Features location-based soil analysis, automated workflow orchestration via n8n, and intelligent explanations powered by Google Gemini.

## 🏗️ Project Architecture

```
├── api/                           # FastAPI REST API
│   └── api.py                    # 3 endpoints: /features, /predict, /explain
├── src/                          # ML pipeline
│   ├── train.py                 # RandomForest model trainer
│   ├── predict.py               # Standalone prediction script
│   └── update.py                # Model update utilities
├── data/
│   └── soil_dataset.csv         # 8000 soil records (6 Tunisian cities)
├── models/
│   ├── crop_model.pkl           # Trained RandomForest classifier
│   └── columns.pkl              # Feature encoding columns
├── n8n-workflow-import.json     # Complete n8n workflow
├── .env                         # Environment variables (API keys)
├── requirements.txt             # Python dependencies
└── README.md
```

## ⚙️ Setup

### 1. Install Dependencies

```bash
pip install -r requirements.txt
```

### 2. Configure Environment Variables

Create/edit `.env` file in project root:

```env
GEMINI_API_KEY=your-google-gemini-api-key
OPENWEATHER_API_KEY=your-openweather-api-key
```

**Get Free API Keys:**
- **Gemini** (free tier): https://makersuite.google.com/app/apikey
- **OpenWeatherMap** (free tier): https://openweathermap.org/api

### 3. Train the Model

```bash
cd src
python train.py
```

Generates:
- `models/crop_model.pkl` - RandomForest model
- `models/columns.pkl` - Feature columns for encoding

## 🚀 Running the System

### Option A: API Only

```bash
cd api
python -m uvicorn api:app --host 0.0.0.0 --port 8001
```

API will be available at `http://localhost:8001`

### Option B: Full Pipeline with n8n (Recommended)

**Terminal 1 - Start n8n:**
```bash
n8n
```
Access at http://localhost:5678

**Terminal 2 - Start FastAPI:**
```bash
cd api
python -m uvicorn api:app --host 0.0.0.0 --port 8001
```

**Terminal 3 - Import workflow:**
1. Open n8n → "Workflows" → "Import"
2. Select `n8n-workflow-import.json`
3. Click "Test workflow"

## 📡 API Endpoints

### GET `/features` - Retrieve soil & weather data

**PowerShell:**
```powershell
$r = Invoke-WebRequest -Uri "http://localhost:8001/features?latitude=36.8065&longitude=10.1815" -UseBasicParsing
$r.Content | ConvertFrom-Json | ConvertTo-Json
```

**Response:**
```json
{
  "location": "Tunis",
  "Temparature": 14.99,
  "Humidity": 67,
  "Moisture": 60.5,
  "Soil_Type": "Loamy",
  "Nitrogen": 18.50,
  "Potassium": 4.22,
  "Phosphorous": 12.1
}
```

### POST `/predict` - Get crop recommendation

**PowerShell:**
```powershell
$headers = @{"Content-Type"="application/json"}
$body = @{
  Temparature = 14.99
  Humidity = 67
  Moisture = 60.5
  Soil_Type = "Loamy"
  Nitrogen = 18.50
  Potassium = 4.22
} | ConvertTo-Json

$r = Invoke-WebRequest -Uri "http://localhost:8001/predict" `
  -Method POST -Headers $headers -Body $body -UseBasicParsing
$r.Content | ConvertFrom-Json | ConvertTo-Json
```

**Response:**
```json
{
  "recommended_crop": "Sugarcane"
}
```

### POST `/explain` - Get LLM explanation

**PowerShell:**
```powershell
$headers = @{"Content-Type"="application/json"}
$body = @{
  crop = "Sugarcane"
  location = "Tunis"
  soil_type = "Loamy"
  temperature = 14.99
  humidity = 67
  nitrogen = 18.50
  potassium = 4.22
} | ConvertTo-Json

$r = Invoke-WebRequest -Uri "http://localhost:8001/explain" `
  -Method POST -Headers $headers -Body $body -UseBasicParsing
$r.Content | ConvertFrom-Json | ConvertTo-Json
```

**Response:**
```json
{
  "crop": "Sugarcane",
  "explanation": "Sugarcane thrives in this region due to the strong humidity levels (67%) and loamy soil which provides optimal drainage..."
}
```

## 🔄 n8n Workflow

**Pipeline Flow:**
1. **Webhook** - Receive latitude/longitude coordinates
2. **Get Features** - Call `/features` endpoint to fetch soil & weather data
3. **Predict Crop** - Call `/predict` with environmental parameters
4. **Explain Crop** - Call `/explain` with prediction + context
5. **Format Response** - Combine all data into structured response
6. **Respond** - Return to user

**Test via PowerShell:**
```powershell
$body = '{"latitude": 36.8065, "longitude": 10.1815}'
$r = Invoke-WebRequest -Uri "http://localhost:5678/webhook-test/agro-predict" -Method POST -Body $body -ContentType "application/json" -UseBasicParsing
$r.Content | ConvertFrom-Json | ConvertTo-Json
```

What n8n gives you here:

  ne entry point for clients: instead of the client calling /features, then /predict, then /explain, it can send just latitude and longitude once.
  Automatic chaining: n8n handles the order of operations and passes data from one step to the next.
  Response aggregation: it combines weather, soil data, prediction, and explanation into one final payload.
  Less frontend/backend glue code: the client does not need to know how to reshape request bodies between the three APIs.
  Easier workflow changes: if you later add logging, notifications, persistence, retries, or another API call, you can insert that in n8n without changing the client contract.
  Better operational visibility: n8n gives you a visual workflow, execution history, and easier debugging for the full pipeline.

## 📊 Supported Locations

Model trained on 6 Tunisian cities:
- Tunis (36.8065, 10.1815)
- Sousse (35.8256, 10.6369)
- Sfax (34.7406, 10.7603)
- Nabeul (36.4561, 10.7376)
- Mednine (33.3549, 10.5055)
- Gabes (33.8815, 10.0982)

Nearest location is automatically selected based on coordinates.

## 🌾 Supported Crops

11 crop types in training data:
Wheat, Rice, Corn, Cotton, Sugarcane, Pulses, Millets, Oils, Spices, Fruits, Vegetables

## Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| ML Model | scikit-learn RandomForest | 1.3.0 |
| REST API | FastAPI | 0.103.0 |
| Workflow Orchestration | n8n | Latest |
| LLM Engine | Google Gemini Pro | Latest |
| Weather Data | OpenWeatherMap API | Free tier |
| Data Processing | pandas | 2.0.3 |
| Model Serialization | joblib | 1.3.1 |

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Model not loaded" | Run `python src/train.py` to train the model |
| "GEMINI_API_KEY not configured" | Add API key to `.env` file and restart API |
| "Port 8001 already in use" | Change port: `uvicorn api:app --port 8002` |
| "Weather data not found" | Verify OpenWeatherMap API key is valid |
| "Connection refused to n8n" | Ensure n8n is running: `n8n` |

##  Dataset Columns

**soil_dataset.csv** structure:
- `location` - City name (Tunis, Sousse, etc..)
- `Temparature` - Temperature in °C
- `Humidity` - Humidity percentage
- `Moisture` - Soil moisture percentage
- `Soil_Type` - Category (Loamy, Sandy, Clay, etc..)
- `Nitrogen` - mg/kg
- `Potassium` - mg/kg
- `Phosphorous` - mg/kg
- `Crop_Type` - Target crop label


## 📚 Future Enhancements


- [ ] Implement model versioning system
- [ ] Add data validation & error handling improvements
- [ ] Unit and integration tests
- [ ] Database integration (PostgreSQL)
- [ ] Additional weather parameters (rainfall, wind)
- [ ] Real-time model retraining pipeline
