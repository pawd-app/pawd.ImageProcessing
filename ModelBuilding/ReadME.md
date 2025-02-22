
# YOLOv8 Model to ONNX Converter

This Python script converts YOLOv8 models (`.pt` files) to ONNX format (`.onnx` files). It processes all `.pt` files in a specified input folder (`/models`) and saves the converted `.onnx` files to an output folder (`/onnx`).

----------

## Table of Contents

1.  [Features](#features)
    
2.  [Requirements](#requirements)
    
3.  [Setup](#setup)
    
4.  [Usage](#usage)
    
5.  [Folder Structure](#folder-structure)
    
6.  [Troubleshooting](#troubleshooting)
        
----------

## Features

-   Converts YOLOv8 PyTorch models (`.pt`) to ONNX format (`.onnx`).
    
-   Processes all `.pt` files in the `/models` folder.
    
-   Saves converted files to the `/onnx` folder with the same name as the input file.
    
-   Includes debugging information for easy troubleshooting.
    

----------

## Requirements

-   Python 3.7 or later
    
-   `ultralytics` library (for YOLOv8)
    
-   `torch` library (PyTorch)
    

Install the required libraries using pip:


```
pip install ultralytics torch
```
----------

## Setup

1.  Clone or download this repository.
    
2.  Place your YOLOv8 `.pt` model files in the `/models` folder.
    
3.  Ensure the folder structure looks like this:
    
    Copy
    
    /project-folder
    ├── convert_to_onnx.py  # The conversion script
    ├── models/             # Folder containing .pt files
    │   ├── yolov8n.pt
    │   ├── yolov8s.pt
    │   └── ...
    └── onnx/               # Folder where ONNX files will be saved
    

----------

## Usage

1.  Open a terminal or command prompt.
    
2.  Navigate to the project folder:
 
   ```
   cd C:\<path_to_project>
   ```
    
3.  Run the script:
    
 ```
    python convert_to_onnx.py
```
    

### Example Output

```
Current working directory: C:\repos\pawd.imageworkers
Models directory 'C:\repos\pawd.imageworkers\models' exists.
Creating output directory: C:\repos\pawd.imageworkers\onnx
Found 2 model files in 'C:\repos\pawd.imageworkers\models'.

Processing model: C:\repos\pawd.imageworkers\models\yolov8n.pt
Loading model...
Model loaded successfully.

Model Summary:
Model: YOLOv8n
Parameters: 3.2M
Layers: 224
...

Output ONNX file will be saved to: C:\repos\pawd.imageworkers\onnx\yolov8n.onnx
Exporting model to ONNX format...
Exported C:\repos\pawd.imageworkers\models\yolov8n.pt to C:\repos\pawd.imageworkers\onnx\yolov8n.onnx

Processing model: C:\repos\pawd.imageworkers\models\yolov8s.pt
Loading model...
Model loaded successfully.

Model Summary:
Model: YOLOv8s
Parameters: 11.2M
Layers: 300
...

Output ONNX file will be saved to: C:\repos\pawd.imageworkers\onnx\yolov8s.onnx
Exporting model to ONNX format...
Exported C:\repos\pawd.imageworkers\models\yolov8s.pt to C:\repos\pawd.imageworkers\onnx\yolov8s.onnx

All models processed.
```
----------

## Folder Structure

After running the script, the folder structure will look like this:

Copy

/project-folder
├── convert_to_onnx.py  # The conversion script
├── models/             # Folder containing .pt files
│   ├── yolov8n.pt
│   ├── yolov8s.pt
│   └── ...
└── onnx/               # Folder containing converted .onnx files
    ├── yolov8n.onnx
    ├── yolov8s.onnx
    └── ...

----------

## Troubleshooting

### 1. **No Models Found**

-   Ensure the `/models` folder exists and contains `.pt` files.
    
-   Verify the current working directory by adding this debug line to the script:
    
    python
    
    Copy
    
    print(f"Current working directory: {os.getcwd()}")
    

### 2. **ONNX Files Not Saved in the Correct Folder**

-   Ensure the `onnx_file` path is correctly constructed:
    
    python
    
    Copy
    
    onnx_file = os.path.join(onnx_dir, f"{model_name}.onnx")
    
-   Check the debug output to confirm the `onnx_file` path.
    

### 3. **Permission Errors**

-   Ensure you have write permissions in the `/onnx` folder.
    
-   Run the script as an administrator if necessary.
    

### 4. **Model Export Fails**

-   Ensure the `.pt` files are valid YOLOv8 models.
    
-   Check for errors in the debug output and verify the `ultralytics` library is installed correctly.
    

----------

## Acknowledgments

-   [Ultralytics](https://ultralytics.com/) for the YOLOv8 implementation.
    
-   [ONNX](https://onnx.ai/) for the open neural network exchange format.
    

----------
