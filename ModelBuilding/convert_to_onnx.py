import os
from glob import glob
from ultralytics import YOLO

# Get the directory where the script is located
script_dir = os.path.dirname(os.path.abspath(__file__))

# Define the input and output directories (relative to the script's location)
models_dir = os.path.join(script_dir, "models")  # Relative path to the models folder
onnx_dir = os.path.join(script_dir, "onnx")      # Relative path to the output folder

# Debug: Print paths for verification
print(f"Script directory: {script_dir}")
print(f"Models directory: {models_dir}")
print(f"ONNX directory: {onnx_dir}")

# Debug: Check if the models directory exists
if os.path.exists(models_dir):
    print(f"Models directory '{models_dir}' exists.")
else:
    print(f"Error: Models directory '{models_dir}' does not exist.")

# Debug: Create the output directory if it doesn't exist
if not os.path.exists(onnx_dir):
    print(f"Creating output directory: {onnx_dir}")
    os.makedirs(onnx_dir, exist_ok=True)
else:
    print(f"Output directory '{onnx_dir}' already exists.")

# Get all .pt files in the /models directory
model_files = glob(os.path.join(models_dir, "*.pt"))
print(f"Found {len(model_files)} model files in '{models_dir}'.")

# Iterate over each model file
for model_file in model_files:
    print(f"\nProcessing model: {model_file}")
    
    # Check if the model file exists
    if not os.path.exists(model_file):
        print(f"Error: Model file '{model_file}' does not exist. Skipping.")
        continue
    
    try:
        # Load the model
        print("Loading model...")
        model = YOLO(model_file)
        print("Model loaded successfully.")
        
        # Print model summary for debugging
        print("\nModel Summary:")
        model.info()
        
        # Define the output path with the same name but .onnx extension
        model_name = os.path.splitext(os.path.basename(model_file))[0]
        onnx_file = os.path.join(onnx_dir, f"{model_name}.onnx")
        print(f"Output ONNX file will be saved to: {onnx_file}")
        
        # Export the model to ONNX format
        print("Exporting model to ONNX format...")
        model.export(format="onnx", name=onnx_file)
        print(f"Exported {model_file} to {onnx_file}")
    
    except Exception as e:
        # Handle any errors during model loading or export
        print(f"Error processing model '{model_file}': {e}")
        continue

print("\nAll models processed.")