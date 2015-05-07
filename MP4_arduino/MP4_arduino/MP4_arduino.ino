/*
  Example Bluetooth Serial Passthrough Sketch
 by: Jim Lindblom
 SparkFun Electronics
 date: February 26, 2013
 license: Public domain

 This example sketch converts an RN-42 bluetooth module to
 communicate at 9600 bps (from 115200), and passes any serial
 data between Serial Monitor and bluetooth module.
 */
#include <SoftwareSerial.h>  

int bluetoothTx = 10;  // TX-O pin of bluetooth mate, Arduino D2
int bluetoothRx = 11;  // RX-I pin of bluetooth mate, Arduino D3

SoftwareSerial bluetooth(bluetoothTx, bluetoothRx);
int motorPorts[] = {4, 3, 5, 2, 7, 9, 6, 8}; // LE, RE, LW, RW, LK, RK, LA, RA

void setup()
{
  Serial.begin(9600);  // Begin the serial monitor at 9600bps

  bluetooth.begin(115200);  // The Bluetooth Mate defaults to 115200bps
  bluetooth.print("$");  // Print three times individually
  bluetooth.print("$");
  bluetooth.print("$");  // Enter command mode
  delay(100);  // Short delay, wait for the Mate to send back CMD
  bluetooth.println("U,9600,N");  // Temporarily Change the baudrate to 9600, no parity
  // 115200 can be too fast at times for NewSoftSerial to relay the data reliably
  bluetooth.begin(9600);  // Start bluetooth serial at 9600
}

int intensity[] = {0, 0, 0, 0};
int tmpIntensity[] = {0, 0, 0, 0};
int intensityIndex = 0;
String inString = "";
int NUM_MOTORS = 4;
void loop()
{  
  if(bluetooth.available())  // If the bluetooth sent any characters
  {
    /*
    char inChar = bluetooth.read();
    inString += inChar;
    if (inChar == ',') {
      double inFloat = inString.toDouble();
      Serial.println(inFloat);
      inString = "";
    }
    */
    
    byte data = bluetooth.read();
    
    if (data == 0){
      Serial.print(data);
      if(intensityIndex == NUM_MOTORS) {
        for(int i=0; i<NUM_MOTORS; i++){
          intensity[i] = tmpIntensity[i];
        }
        Serial.print("<<");
      }
      Serial.println();
      intensityIndex = 0;
    } else {
      if(intensityIndex < NUM_MOTORS){
        tmpIntensity[intensityIndex] = data;
        Serial.print(tmpIntensity[intensityIndex]);
        Serial.print("    ");
      } else{
        Serial.print(data);
        Serial.print("    ");
      }
      intensityIndex++;
    }
  }
  
  if(Serial.available())  // If stuff was typed in the serial monitor
  {
    // Send any characters the Serial monitor prints to the bluetooth
    char a = Serial.read();
    bluetooth.print(a);
    Serial.print("Sent: ");
    Serial.println(a);
  }
  
  // and loop forever and ever!
  for (int i=0; i<NUM_MOTORS; i++){
    analogWrite(motorPorts[i], intensity[i]);
  }
}

void printIntensity(){
    Serial.print(">>");
    for(int i=0; i<8; i++){
      Serial.print(intensity[i]);
      Serial.print("  ");
    }
    Serial.println();
}

