# Perception Overview
Time spent actually "coding": <1%
Time spent networking: way too much

For this module I had to download python and setup a venv environment, activate the environment (venv/Scripts/activate.bat in command prompt), then install ultralytics, opencv-python, opencv-contrib-python, and roslibpy with pip. Four 4x4 ArUco markers are needed as well, to be placed in the corners of the environment for use in calibration. You will also need to, using Windows Subsystem for Linux, install Ubuntu 22.04 and an insane amount of ROS stuff. Unity will need to be configured to communicate with ROS. All terminal commands are provided (I think?). It will take some time.

For running this code, on an already configured machine and an already configured Unity project, you need to run these commands:

On windows, if you're using WSL as outlined in this README, get your Ubuntu hostname and save it for use in Perception.py, either during runtime or by hardcoding it into the initialization of the ArenaTracker class.

For the ROS-TCP-Unity connection, in an Ubuntu terminal:
source ~/unity_ros_ws/install/setup.bash (Skip potentially if you ran the echo command)
ros2 run ros_tcp_endpoint default_server_endpoint --ros-args -p ROS_IP:=<YOUR_UBUNTU_IP>

For Python Rosbridge, in another Ubuntu terminal:
ros2 run rosbridge_server rosbridge_websocket
UNNECESSARY: can setup a debug listener on the rosbridge connection, after python code is run, with: ros2 topic echo /arena/detections

## Perception.py
The Perception.py script relies HEAVILY on the aforementioned libraries (ultralytics, opencv) to do pretty much everything. Honestly, I wish there was more to say, it's just organizing function calls and storing them as variables and calling them as needed. Most lines are commented, but I'll explain the general flow:
Pretty much everything exists under the ArenaTracker object. Most permenant data is stored in the variable self, INCLUDING the initialization of the YOLO model. Changing models is as simple as changing the string in initialization. Past initialization, program flow occurs in the run class function, where a while True loop repeatedly takes frames from the webcam, infers objects with the model, then gives outputs by drawing on the frame and putting text on the console before imshow-ing a named window "Arena Perception." Pressing R in the window will reset the calibration (look for the ArUco markers again and remake the homography matrix), and Q will close the window. Calibration is called in the run function and is handled by another function "get_perspective_transform." I wish there was more to say for this part, since it is very cool, but honestly all it does is call functions. It looks for four 4x4 ArUco markers interpreted to have id's 0, 1, 2, and 3 arranged in a rectangular Z (top-left, top-right, bottom-left, bottom-right) configuration in the frame (it will wait until the markers are visible). You can download these markers online and print them out. The expected height and width of the rectangle is decided in the initialization step, hard-coded by the user (change to 4mx4m on test day). Then, once all 4 are successfully identified, it puts the positon (centers) of the markers and stores them in an array, and then calls a function to produce the homography matrix based on these centers. The homography matrix is a transformation matrix that turns pixel coordinates into real coordinates, measured in meters. A function pixels_to_meter takes given pixel coordinates and converts them to relative real coordinates (in meters) by using the homography matrix. At this point, there really isn't much else to say. YOLO does a one-line inference and gives the corners of the bounding box of each thing it detects in pixel coordinates (in some object, passed to "results"), and the "position" of the object is assumed to be in the middle of the bounding box, so the "positon" of the object within the arena is wherever the middle of the bounding box for that object is. Once those coordinates are passed through the matrix, they can be given to Unity.

Setting self.debug = 1 in the ArenaTracker initialization makes it so the program draws the position of the cursor in the window, and prints the position of the cursor (in interpreted real coordinates) to the terminal. If calibration was successful and everything is working as intended, then putting the cursor in the middle of the ID = 0 ArUco marker in the camera's frame should give (0,0) for the mouse position.

## Unity, Linux, and ROS nonsense
Had to use Windows Subsystem for Linux to install Ubuntu 22.04 and ROS 2 Humble for the Unity-Python connection. Instructions:

In the relevant unity project, go windows -> package manager -> + -> add package from git URL, then paste https://github.com/Unity-Technologies/ROS-TCP-Connector.git?path=/com.unity.robotics.ros-tcp-connector and add.

In (Administrator) Powershell, do wsl --install -d Ubuntu-22.04, you might need to restart and then open "Ubuntu" in the start menu, or try running the command again. whats important is to get into the Ubuntu terminal somehow and create a unix username and password. Make the password something easy to type like 1234
Now to download ROS 2 Humble, in the Ubuntu terminal: NOTE, if you're in the actual Ubuntu terminal, right-clicking should paste the clipboard.

sudo apt update && sudo apt install locales
sudo locale-gen en_US en_US.UTF-8
sudo update-locale LC_ALL=en_US.UTF-8 LANG=en_US.UTF-8
export LANG=en_US.UTF-8

sudo apt update && sudo apt install curl -y
sudo curl -sSL https://raw.githubusercontent.com/ros/rosdistro/master/ros.key -o /usr/share/keyrings/ros-archive-keyring.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/ros-archive-keyring.gpg] http://packages.ros.org/ros2/ubuntu $(. /etc/os-release && echo $UBUNTU_CODENAME) main" | sudo tee /etc/apt/sources.list.d/ros2.list > /dev/null

(This one takes a few minutes)
sudo apt update
sudo apt install ros-humble-desktop -y

(This one makes it so ROS 2 loads every time you open Ubuntu)
echo "source /opt/ros/humble/setup.bash" >> ~/.bashrc
source ~/.bashrc

To test if its working: ROS comes with demo talker/listener nodes. In Ubuntu:

ros2 run demo_nodes_cpp talker
ros2 run demo_nodes_py listener

see if the listener can hear the talker.

Then had to make a ROS workspace to build the workspace to make the ROS-TCP-Endpoint node (Unity cannot natively communicate with ROS but can communicate in TCP channels, so have to translate).
In Ubuntu,

Installing build tools:
sudo apt install python3-colcon-common-extensions git -y

Creating the workspace:
mkdir -p ~/unity_ros_ws/src
cd ~/unity_ros_ws/src

Cloning:
git clone -b main-ros2 https://github.com/Unity-Technologies/ROS-TCP-Endpoint.git

Building the workspace:
cd ~/unity_ros_ws
colcon build

source install/setup.bash

Now turn it on. This will open a terminal listening to port 10000.
ros2 run ros_tcp_endpoint default_server_endpoint --ros-args -p ROS_IP:=0.0.0.0

NOTE: Anytime you want to use this workspace in the Ubuntu terminal after closing the terminal, You will need to redo

cd ~/unity_ros_ws <-- Going to the directory containing the workspace
source install/setup.bash <-- setting up the terminal to accept the command? I think?
and then run the relevant "ros2 run ..." command.
A permenant fix is to do
echo "source ~/unity_ros_ws/install/setup.bash" >> ~/.bashrc    >-- ~./bashrc is the script that runs every time you open a terminal window. It will automatically play the command after "echo", meaning you dont have to run it every time you open a terminal.
After doing that, you can just run the "ros2 run..." command. For other ROS workspaces, you'd probably need to configure your ubuntu terminal to run their setup.bash files, too. I really have no idea.



Back to the project:
If the "ros2 run..." command works, then press Ctrl + C, or close the terminal and reopen it. Do hostname -I, and note the IP address.
Open Unity, and at the top there's a section called "Robotics." In ROS settings, change the protocol to ROS2, the ROS IP Address to the IP address given by the hostname command in the Ubuntu terminal (this is the specific IP address of the ubuntu terminal on your machine, we give it this one explicitly because sometimes windows doesn't automatically route WSL to localhost), and ROS port to 10000.

Also, create an empty object in the hierarchy called ROS_Manager, and add a component in the inspector. The component type is "ROS Connection." Enable show HUD, and set the ROS IP Address (again) to the one you got from the Ubuntu terminal. Make sure port is still 10000.

Now rerun the "ros2 run..." command , but change the final ROS_IP: argument to the found hostname IP address:
ros2 run ros_tcp_endpoint default_server_endpoint --ros-args -p ROS_IP:=<YOUR_UBUNTU_IP>
note: if the port isn't automatically setting to 10000, try setting it manually at the end of the ip address with :10000.
Press play in Unity, and there should be 1. A UI element in the top right of the game screen, displaying the ROS connection IP, 2. No errors in the Unity console, 3. A successful connection message in the Ubuntu terminal.
If that works, then ROS is successfully speaking to Unity.



## Python, Linux, and ROS nonsense (phase 2)

Now we need to get Python (Windows) talking to ROS (inside WSL) using a new workaround: Rosbridge. It sure is fun finding workarounds, libraries, and plugins to get native windows programs to talk to a linux virtual machine, just for Linux to be used to communicate with other programs in windows.
Open up a new Ubuntu terminal (if you want to keep the server running), or close the server with Ctrl + C.

In Ubuntu:
Installing the package:
sudo apt install ros-humble-rosbridge-server -y
Running the bridge:
ros2 run rosbridge_server rosbridge_websocket

