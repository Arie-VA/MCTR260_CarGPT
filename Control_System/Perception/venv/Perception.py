import cv2
import numpy as np
from ultralytics import YOLO
import roslibpy
import json

class ArenaTracker:
    def __init__(self, camera_index = 0):
        
        # Set self.debug to be 1 to enable debugging mode.
        self.debug = 0
        
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

        # ArUco setup:
        self.aruco_dict = cv2.aruco.getPredefinedDictionary(cv2.aruco.DICT_4X4_50)
        self.aruco_params = cv2.aruco.DetectorParameters()

        # Arena Real-World Dimensions (Meters)
        # 0,0 is top left
        self.arena_width = 1.0
        self.arena_height = 1.0
        # The Destination Points (Real World)
        # It orders from top left, top right, bottom left, bottom right.
        self.dst_points = np.array([[0,0],[self.arena_width, 0],[0,self.arena_height],[self.arena_width,self.arena_height]],dtype=np.float32)

        # Initializing a spot to put the homography matrix within self.
        # A homography matrix is a tool that will help map what the computer sees onto a 2d plane, so that we can extract the relevant coordinates (YOLO gives coordinates as pixel positions)
        self.homography_matrix = None
        
        # Debug
        self.mouse_xy = (0,0)
        
        # ROS2 Connection nonsense. Harcode IP here under skipstep == 1, if you don't feel like inputting it every time. This changes on a per-machine basis.
        # Picking an IP address:
        skipstep = 1
        if skipstep == 1:
            UbuntuIP = '172.19.211.125'    
        else:
            UbuntuIP = input("Enter the Linux IP that ROS is running on (Ubuntu Terminal from README): ")
        
        # Connecting to ROS: ros_client is the object under self that represents the ROS connection.
        print(f"Connecting to ROS2, at IP {UbuntuIP}")
        self.ros_client = roslibpy.Ros(host=UbuntuIP, port=9090)
        # Where to publish: ros_topic is the object that represents where Python will put the data.
        self.ros_topic = roslibpy.Topic(self.ros_client, '/arena/detections', 'std_msgs/String') # We publish it as a json string.

        self.ros_client.run()
        if self.ros_client.is_connected:
            print("ROS2 Connection Successful.")
        else:
            print("Failed to connect to ROS2. Ensure rosbridge is running.")
        


    def mouse_callback(self, event, x, y, flags, param):
        # Debug function: If used, prints the position of the cursor on the screen to the terminal (relative to coordinate system), for debugging coordinate positions.
        if event == cv2.EVENT_MOUSEMOVE:
            self.mouse_xy = (x ,y)
        

    def get_perspective_transform(self,frame):
        # Detects ArUco Markers an calculates the homography matrix.
        corners, ids, rejected = cv2.aruco.detectMarkers(frame, self.aruco_dict, parameters=self.aruco_params)
        
        if ids is None:
            return None
        else:
            cv2.aruco.drawDetectedMarkers(frame,corners,ids)
        
        ids = ids.flatten()

        # Need all 4 corner markers with ID 0-3
        if all(id in ids for id in [0,1,2,3]):
            src_points = []
            #Extract the center of each marker in order 0, 1, 2, 3
            for i in range(4):
                # Find the index of ID 'i' in the detection list 
                index = np.where(ids == i)[0][0]
                # Get the corner coordinates of that marker
                c = corners[index][0]
                #Calculate the center of the marker
                cx = int(np.mean(c[:,0]))
                cy = int(np.mean(c[:,1]))
                src_points.append([cx,cy])
            src_points_array = np.array(src_points, dtype = np.float32) # At this point, I think src_points is an array of size 1 containing an array of size 4 which are themselves array of size 2?
            #Compute Homography Matrix to map pixels to meters
            self.homography_matrix = cv2.getPerspectiveTransform(src_points_array, self.dst_points)
            print("Calibration Successful, Matrix Locked.")
            cv2.polylines(frame, [src_points_array[[0,1,3,2]].astype(np.int32)], True, (0, 255, 0), 3)
            return True
        return False
    
    def pixel_to_meter(self,u,v):
        # converts pixel coordinates (u,v) to real meters.
        
        if self.homography_matrix is None:
            return (-1,-1)
        
        # I think we wrap point in a series of arrays because it has to be compatible with the format of self.homography_matrix.
        point = np.array([[[u,v]]],dtype=np.float32)
        transformed_point = cv2.perspectiveTransform(point,self.homography_matrix) #God I love libraries. literally "point = point.transformed" lmao.

        x = transformed_point[0][0][0]
        y = transformed_point[0][0][1]
        
        return round(x,4), round(y,4)

    def get_center(self, x1, y1, x2, y2):
        # The model gives the top left an bottom right pixel coordinates of the bounding box in xyxy format, this function just gives the middle of the bounding box in question.
        cx = int((x1+x2)/2)
        cy = int((y1+y2)/2)
        return cx,cy
    
    def processs_detections(self,frame,result):
        # UNUSED


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
        print("Ensure camera angle.")
        cv2.namedWindow("Arena Perception")
        # Necessary for Debugging, set mouse_callback as the variable for mouse callback.
        if self.debug == 1:
            cv2.setMouseCallback("Arena Perception", self.mouse_callback)
        while True:
            # ret is a bool that says if the operation is successful, frame is a Matlike that contains pixel information. Capture a frame.
            ret, frame = self.cap.read()
            if not ret: break

            # Calibration:
            if self.homography_matrix is None:
                success = self.get_perspective_transform(frame)
                if success:
                    cv2.putText(frame, "CALIBRATED",(20,50),cv2.FONT_HERSHEY_SIMPLEX, 1, (0,255,0), 2)
                else:
                    cv2.putText(frame, "Searching for Markers 0-3 ...", (20,50), cv2.FONT_HERSHEY_SIMPLEX,0.7,(0,0,255),2)
            
            else:
                # If we make it into this block, calibration is complete.
                # Run an inference on the frame.
                results = self.model(frame,verbose = False, conf = 0.5)

                if self.debug == 1:
                    mx, my = self.mouse_xy
                    real_mx, real_my = self.pixel_to_meter(mx,my)
                    cv2.circle(frame,(mx,my),1,(0,255,0))
                    debug_text = f"Mouse: ({real_mx:.2f}m, {real_my:.2f}m)"
                    print(debug_text)




                for box in results[0].boxes:
                    # For each bounding box, it means. Not physical boxes. Runs through each item.
                    #x1,y1,x2,y2 are the top left and bottom right corners of the bounding box detected by the model. Have to map to int, since xyxy is natively some array
                    x1,y1,x2,y2 = map(int,box.xyxy[0])
                    # Still in pixels.
                    cx, cy = int((x1+x2)/2),int((y1+y2)/2)
                    
                    # Now convert to meters.
                    real_x, real_y = self.pixel_to_meter(cx, cy)

                    # Draw on the frame.
                    # class_name is the type name of the detected object
                    class_name = self.model.names[int(box.cls[0])]
                    #Draw a bouning box
                    cv2.rectangle(frame,(x1,y1),(x2,y2),(255,0,0),2)

                    # Display the real-world coordinates
                    label = f"{class_name}: ({real_x:.2f}m, {real_y:.2f}m)"
                    cv2.putText(frame,label,(x1,y1-10),cv2.FONT_HERSHEY_SIMPLEX,0.5,(255,255,0),2)

                    print(f"Object: {class_name} at ({real_x:.2f}m, {real_y:.2f}m)")

                    if self.ros_client.is_connected:
                        # Which data gets published?
                        data_dict = {
                            "label": class_name,
                            "x": float(real_x),
                            "y": float(real_y)

                        }
                        # Convert to json and publish through ROS.
                        json_str = json.dumps(data_dict)
                        self.ros_topic.publish(roslibpy.Message({'data':json_str}))

            cv2.imshow("Arena Perception",frame)
            # This line is strange but it forces the keypress into 8 bit and then compares it to the 8 bit ordinal q.
            key = cv2.waitKey(1) & 0xFF 
            if key == ord('q'):
                break
            elif key == ord('r'):
                self.homography_matrix = None

        self.cap.release()
        cv2.destroyAllWindows()
        self.ros_topic.unadvertise()
        self.ros_client.terminate()

if __name__ == "__main__":
    try:
        tracker = ArenaTracker(camera_index = 0) # Change this index to go through system cameras.
        tracker.run()
    except Exception as e:
        print(f"Error: {e}")