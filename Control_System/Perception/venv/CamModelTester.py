import cv2
from ultralytics import YOLO

class ArenaTracker:
    def __init__(self, camera_index = 0):
        #Initializing the camera 

        #Creating an object self that uses the CV library to open the system camera at the given index. 0 is typically the laptop camera.
        self.cap = cv2.VideoCapture(camera_index)

        if not self.cap.isOpened():
            raise ValueError(f"Could not open video device {camera_index}")

        # Loading the YOLO model. Initially, trying to use the premade model yolov8n.pt, which will autodownload. Very small and fast.
        # The model is loaded to the object, self.
        print("Loading YOLO...")
        self.model = YOLO('yolov8n.pt')
        print("Model loaded.")

    def run(self):
        # Main loop for the ArenaTracker entity.
        print("Starting Stream, q to exit")

        while True:
            # Read a frame (check that it worked), run an "inference on the frame", visualize the results with the bounding boxes drawn.
            ret, frame = self.cap.read()

            if not ret:
                print("Failed to grab frame.")
                break

            # Conf controls the confidence level required by the model for a detection.
            results = self.model(frame, verbose=False, conf = 0.5)
            annotated_frame = results[0].plot()

            # Now show the image.
            cv2.imshow('Arena Perception', annotated_frame)

            if cv2.waitKey(1) & 0xFF == ord('q'):
                break
    
        self.cap.release()
        cv2.destroyAllWindows()

if __name__ == "__main__":
    try:
        tracker = ArenaTracker(camera_index = 1) # Change this index to go through system cameras.
        tracker.run()
    except Exception as e:
        print(f"Error: {e}")



