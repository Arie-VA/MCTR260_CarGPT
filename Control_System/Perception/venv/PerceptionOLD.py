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

        # Initializing a spot to put the homography matrix within self.
        # A homography matrix is a tool that will help map what the computer sees onto a 2d plane, so that we can extract the relevant coordinates (YOLO gives coordinates as pixel positions)
        self.homography_matrix = None

    def get_center(self, x1, y1, x2, y2):
        # The model gives the top left an bottom right pixel coordinates of the bounding box in xyxy format, this function just gives the middle of the bounding box in question.
        cx = int((x1+x2)/2)
        cy = int((y1+y2)/2)
        return cx,cy
    
    def processs_detections(self,frame,result):
        # Extracts data from YOLO results and gives custom info we're interested in.
        # YOLO results are a list of frames.
        results = result[0]
        # results.boxes returns all of the bounding boxes detected in the frame result[0]
        boxes = results.boxes

        for box in boxes:
            # box.xyxy is a tensor, need to use map to return them as int.
            x1, y1, x2, y2 = map(int, box.xyxy[0])

            # Get the class ID, confidence, class of object
            cls_id = int(box.cls[0])
            conf = float(box.conf[0])
            class_name = self.model.names[cls_id]

            cx,cy = self.get_center(x1,y1,x2,y2)

            # Make changes to the passed frame.
            #Draw the bounding box
            cv2.rectangle(frame,(x1,y1),(x2,y2),(0,22,0),2)
            #Draw the center point
            cv2.circle(frame,(cx,cy),5,(0,0,255),-1)
            #Draw the Coordinate Text (in pixels)
            text = f"{class_name}: ({cx},{cy}) px"
            cv2.putText(frame,text, (x1,y1 - 10), cv2.FONT_HERSHEY_SIMPLEX,0.5, (0,255,0), 2)

            #Eventually, send data to ROS from here. For now, just print.
            print(f"Detected {class_name} at Pixel: ({cx}, {cy})")
        return frame




    
    def run(self):
        # Main loop for the ArenaTracker entity.
        print("Starting Stream, q to exit")

        while True:
            # Read a frame (check that it worked), run an "inference" on the frame (let the model look), visualize the results with the bounding boxes drawn.
            ret, frame = self.cap.read()

            if not ret:
                print("Failed to grab frame.")
                break
            
            # Run the inference
            # Conf controls the confidence level required by the model for a detection.
            results = self.model(frame, verbose=False, conf = 0.5)
            # Run our custom defined function process_detections to draw on the frame.
            annotated_frame = self.processs_detections(frame,results)

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