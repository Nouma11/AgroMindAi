"""
Agricultural ML Model Training Script
Trains RandomForest classifiers for crop and fertilizer recommendation.

Phase 1: Uses all 7 features + trains fertilizer model as second target
Phase 2: Stratified split + 5-fold cross-validation + confusion matrix
Phase 3: Hyperparameter tuning via RandomizedSearchCV
Phase 4: Feature engineering (N:P:K ratios, temp*humidity interaction)
"""
import pandas as pd
import numpy as np
import joblib
import os
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import seaborn as sns
from sklearn.metrics import classification_report, confusion_matrix
from sklearn.model_selection import (
    train_test_split,
    StratifiedKFold,
    RandomizedSearchCV,
    cross_val_score,
)
from sklearn.ensemble import RandomForestClassifier


os.makedirs("../models", exist_ok=True)


try:
    df = pd.read_csv("../data/soil_dataset.csv")
except FileNotFoundError:
    print("Error: ../data/soil_dataset.csv not found")
    exit(1)

print(f"Dataset shape: {df.shape}")
print(df.head())


feature_cols = ["Temparature", "Humidity", "Moisture", "Soil_Type",
                "Nitrogen", "Potassium", "Phosphorous"]

X = df[feature_cols].copy()

X["N_K_ratio"] = X["Nitrogen"] / (X["Potassium"] + 1)
X["P_N_ratio"] = X["Phosphorous"] / (X["Nitrogen"] + 1)
X["Temp_Humidity"] = X["Temparature"] * X["Humidity"]

X = pd.get_dummies(X)

joblib.dump(X.columns, "../models/columns.pkl")


y_crop = df["Crop_Type"]
y_fert = df["Fertilizer Name"]

print(f"\nCrop distribution:\n{y_crop.value_counts()}")
print(f"\nFertilizer distribution:\n{y_fert.value_counts()}")


X_train, X_test, y_crop_train, y_crop_test, y_fert_train, y_fert_test = (
    train_test_split(X, y_crop, y_fert, test_size=0.2, random_state=42, stratify=y_crop)
)


param_dist = {
    "n_estimators": [100, 300, 500],
    "max_depth": [None, 15, 20, 30],
    "min_samples_split": [2, 5, 10],
    "min_samples_leaf": [1, 2, 4],
    "max_features": ["sqrt", "log2"],
}

cv = StratifiedKFold(n_splits=5, shuffle=True, random_state=42)

print("\n" + "=" * 60)
print("CROP MODEL - Hyperparameter Tuning")
print("=" * 60)

crop_search = RandomizedSearchCV(
    RandomForestClassifier(random_state=42, class_weight="balanced"),
    param_distributions=param_dist,
    n_iter=30,
    cv=cv,
    scoring="f1_weighted",
    random_state=42,
    n_jobs=-1,
    verbose=1,
)
crop_search.fit(X_train, y_crop_train)

crop_model = crop_search.best_estimator_
print(f"\nBest crop model params: {crop_search.best_params_}")
print(f"Best crop CV F1 (weighted): {crop_search.best_score_:.4f}")


crop_cv_scores = cross_val_score(crop_model, X, y_crop, cv=cv, scoring="f1_weighted")
print(f"5-Fold CV F1 (full data): {crop_cv_scores.mean():.4f} ± {crop_cv_scores.std():.4f}")


crop_preds = crop_model.predict(X_test)
print("\nCrop Classification Report:")
print(classification_report(y_crop_test, crop_preds))


cm_crop = confusion_matrix(y_crop_test, crop_preds, labels=crop_model.classes_)
plt.figure(figsize=(12, 10))
sns.heatmap(cm_crop, annot=True, fmt="d", cmap="Blues",
            xticklabels=crop_model.classes_, yticklabels=crop_model.classes_)
plt.title("Crop Prediction - Confusion Matrix")
plt.xlabel("Predicted")
plt.ylabel("Actual")
plt.tight_layout()
plt.savefig("../models/crop_confusion_matrix.png", dpi=150)
plt.close()
print("Saved: models/crop_confusion_matrix.png")


# ===== FERTILIZER MODEL =====
print("\n" + "=" * 60)
print("FERTILIZER MODEL - Hyperparameter Tuning")
print("=" * 60)

fert_search = RandomizedSearchCV(
    RandomForestClassifier(random_state=42, class_weight="balanced"),
    param_distributions=param_dist,
    n_iter=30,
    cv=StratifiedKFold(n_splits=5, shuffle=True, random_state=42),
    scoring="f1_weighted",
    random_state=42,
    n_jobs=-1,
    verbose=1,
)
fert_search.fit(X_train, y_fert_train)

fert_model = fert_search.best_estimator_
print(f"\nBest fertilizer model params: {fert_search.best_params_}")
print(f"Best fertilizer CV F1 (weighted): {fert_search.best_score_:.4f}")


# Cross-validation scores
fert_cv_scores = cross_val_score(
    fert_model, X, y_fert,
    cv=StratifiedKFold(n_splits=5, shuffle=True, random_state=42),
    scoring="f1_weighted",
)
print(f"5-Fold CV F1 (full data): {fert_cv_scores.mean():.4f} ± {fert_cv_scores.std():.4f}")

# Test set evaluation
fert_preds = fert_model.predict(X_test)
print("\nFertilizer Classification Report:")
print(classification_report(y_fert_test, fert_preds))

# Confusion matrix
cm_fert = confusion_matrix(y_fert_test, fert_preds, labels=fert_model.classes_)
plt.figure(figsize=(10, 8))
sns.heatmap(cm_fert, annot=True, fmt="d", cmap="Greens",
            xticklabels=fert_model.classes_, yticklabels=fert_model.classes_)
plt.title("Fertilizer Prediction - Confusion Matrix")
plt.xlabel("Predicted")
plt.ylabel("Actual")
plt.tight_layout()
plt.savefig("../models/fertilizer_confusion_matrix.png", dpi=150)
plt.close()
print("Saved: models/fertilizer_confusion_matrix.png")

# --- Save models ---
joblib.dump(crop_model, "../models/crop_model.pkl")
joblib.dump(fert_model, "../models/fertilizer_model.pkl")

print("\n" + "=" * 60)
print("Both models saved successfully!")
print(f"  Crop model:       models/crop_model.pkl")
print(f"  Fertilizer model: models/fertilizer_model.pkl")
print(f"  Columns:          models/columns.pkl")
print("=" * 60)

