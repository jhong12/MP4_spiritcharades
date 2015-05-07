# SpiritCharades
## Overview
SpiritCharades is a game played by two users with two Kinects. A user (user A) pose in front of the Kinect. The other user (user B) also stands in front of the other Kinect and should not see the pose. The user B guesses the pose and moves his / her arms. The haptic motors attached to the elbow and wrist of each arm vibrates to guide the user B to move the arms close to the user A's arms. The intensity of vibration is stronger when the pose of user B is more different from user A's pose. The similarity of pose is calculated by comparing the distances from the chest to the elbow/wrist. When a user poses same as the user A, all motors stop vibrating.

## How to run
Connect the 2 Kinects to 2 different computers. The two computers run the same Kinect application 'Spirit Charades_KInect'. The Kinect application sends signal to the smartphone applicatoin, MP4Bluetooth, via UDP network protocol. The smartphone sends signal to the Arduino with Sparkfun Bluesmirf. The Arduino code is 'MP4_arduino'.

## Environment
Windows 8

Android 5.0 or above
