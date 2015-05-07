package com.example.mp4bluetooth;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.io.OutputStreamWriter;
import java.util.Set;
import java.util.UUID;
import java.io.*;
import java.net.*;


import android.support.v7.app.ActionBarActivity;
import android.annotation.SuppressLint;
import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.BluetoothServerSocket;
import android.bluetooth.BluetoothSocket;
import android.content.Context;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.os.Bundle;
import android.util.Log;
import android.view.Menu;
import android.view.MenuItem;
import android.view.MotionEvent;
import android.view.View;
import android.view.WindowManager;


public class MainActivity extends ActionBarActivity {
	BluetoothAdapter mBluetoothAdapter;
	BluetoothDevice arduinoDev;
	BluetoothServerSocket remoteServerSocket;
	BluetoothSocket remoteSocket = null;
    InputStream remoteInStream;
    OutputStream remoteOutStream;
    BufferedWriter bw_remote;
    BufferedReader br_remote;
    String MY_UUID_REMOTE = "00001101-0000-1000-8000-00805f9b34fb";
	boolean remoteConnected = false;

	DatagramSocket serverSocket1;
	DatagramSocket serverSocket2;
	
	DatagramSocket clientSocket;
	InetAddress ipAddress;
	double[][] xyz1 = {{0, 0, 0},{0, 0, 0},{0, 0, 0},{0, 0, 0},{0, 0, 0},{0, 0, 0},{0, 0, 0},{0, 0, 0}};
	double[][] xyz2 = {{0, 0, 0},{0, 0, 0},{0, 0, 0},{0, 0, 0},{0, 0, 0},{0, 0, 0},{0, 0, 0},{0, 0, 0}};
	byte[] sendByte = new byte[16];
	
	class MyCanvas extends View{
		Paint bgPaint = new Paint();
		Paint msgPaint = new Paint();
		public MyCanvas(Context context) {
			super(context);
			// TODO Auto-generated constructor stub
			bgPaint.setColor(Color.WHITE);
			msgPaint.setColor(Color.BLACK);
			msgPaint.setTextSize(70);
		}
		
		public void onDraw(Canvas canvas){
			canvas.drawRect(0, 0, 1000, 10000, bgPaint);
			canvas.drawText("BT_CONNECTION:", 100, 200, msgPaint);
			canvas.drawText("   " + statusMessage_remote, 100, 300, msgPaint);
			canvas.drawText("UDP1_MESSAGE:", 100, 450, msgPaint);
			//canvas.drawText("   " + msg, 100, 550, msgPaint);
			canvas.drawText("   " + xyz1[0][0]+","+xyz1[0][1]+","+xyz1[0][2], 100, 550, msgPaint);
			canvas.drawText("UDP2_MESSAGE:", 100, 700, msgPaint);
			//canvas.drawText("   " + msg2, 100, 800, msgPaint);
			canvas.drawText("   " + xyz2[0][0]+","+xyz2[0][1]+","+xyz2[0][2], 100, 800, msgPaint);
			canvas.drawText("UDP_CONNECTION:", 100, 950, msgPaint);
			canvas.drawText("   " + statusMessage_UDP, 100, 1050, msgPaint);
			
			canvas.drawText("BT_MESSAGE:", 100, 1200, msgPaint);
			canvas.drawText("   [int]" + String.format("%1$,.2f", intensity) +",[intInt]"+intensityInteger+",[yb]"+yb, 100, 1300, msgPaint);
		}
	}
	MyCanvas mCanvas;

	RemoteClientThread rst;
	RemoteServerThread rst2;
	String msg="-";
	String msg2="-";
	int connectionTrials = 0;
	long startTrialTime = 0;
	String statusMessage_remote = "";
	String statusMessage_UDP = "";
	double intensity=0;
	int intensityInteger=0;
	byte yb = 0;
	public class RemoteClientThread extends Thread{
		public RemoteClientThread(){
			
		}
		
		long prevTime = -1;
		@SuppressLint("NewApi") public void run(){
			startTrialTime = System.currentTimeMillis();
			try{
				serverSocket1 = new DatagramSocket(5001);
			}catch(Exception e){
				e.printStackTrace();
			}
			
			
			if(arduinoDev!=null){
				while(running){
					try{
						if(remoteConnected){
							if(remoteSocket!=null){
								if(!remoteSocket.isConnected()){
									remoteConnected = false;
									continue;
								}
								
								/*
								if(br_remote.ready())
									msg = br_remote.readLine();
									*/
								
								byte[] receiveData1 = new byte[1024];
								DatagramPacket receivePacket = new DatagramPacket(receiveData1, receiveData1.length);
								//statusMessage_UDP = "waiting...";
								
				                serverSocket1.receive(receivePacket);
							    msg += new String(receiveData1, 0, receivePacket.getLength());
							    statusMessage_UDP = msg;
							    //statusMessage_UDP = "Sending!";

								Log.i("MSG_1", msg);
							    String[] words = msg.split(":");
							    if (words.length >= 4){
							    	for(int part = 0; part < words.length; part++){
							    		String[] cXYZ = words[part].split(",");
							    		if (cXYZ.length < 4) continue;
							    		int whatPart = getPart(cXYZ[0]);
							    		//Log.i("MSG_1_whatpart", whatPart+"...");

							    		for(int i=1; i<cXYZ.length; i++){
							    			xyz1[whatPart][i-1] = Double.parseDouble(cXYZ[i]);
							    		}
							    	}
							    	
							    	for(int i=0; i<4; i++){
							    		double sum = 0;
							    		for (int j=0; j<3; j++){
							    			sum += (xyz1[i][j] - xyz2[i][j])
							    					*(xyz1[i][j] - xyz2[i][j]);
							    		}
							    		
							    		double locIntensity = Math.sqrt(sum)*200 + 155;
							    		if (locIntensity > 255) locIntensity = 255;
							    		else if (locIntensity < 180) locIntensity = 2;
							    		int locIntensityInteger = (int)locIntensity;
							    		byte locYb = (byte)(locIntensityInteger);

							    		if (i == 0) {
							    			intensity = locIntensity;
							    			intensityInteger = locIntensityInteger;
							    			yb = (byte) (locYb & 0xFF);
							    		}
							    		/*
							    		Log.i("BT_MSG", "["+xyz1[i][0]+","+xyz2[i][0]+"]["
							    							+xyz1[i][1]+","+xyz2[i][1]+"]["
							    							+xyz1[i][2]+","+xyz2[i][2]+"]"+locIntensityInteger+":::"+locYb+":::"+i);
							    		*/
							    		remoteOutStream.write(locYb);
							    		remoteOutStream.flush();
							    	}
							    	remoteOutStream.write((byte)0);
							    	remoteOutStream.flush();
							    	msg = "";
							    }
							    
								
								mCanvas.postInvalidate();

							}
						}else {
							//if(remoteSocket!=null) remoteSocket.close();
							remoteSocket = arduinoDev.createRfcommSocketToServiceRecord(UUID.fromString(MY_UUID_REMOTE));
							statusMessage_remote = "Waiting..."+connectionTrials;
							connectionTrials++;
							Log.i("TAG", "Waiting...");
							mCanvas.postInvalidate();
							remoteSocket.connect();
							
							if(remoteSocket.isConnected()){									
								statusMessage_remote = "Connected!!" + (System.currentTimeMillis()-startTrialTime);
								remoteInStream = remoteSocket.getInputStream();
								remoteOutStream = remoteSocket.getOutputStream();	
								br_remote = new BufferedReader(new InputStreamReader(remoteInStream));
								bw_remote = new BufferedWriter(new OutputStreamWriter(remoteOutStream));
								remoteConnected = true;
								connectionTrials = 0;
							}
						}
						mCanvas.postInvalidate();
					}catch(Exception e){
						e.printStackTrace();
						//remoteConnected = false;
					}
				}
			}else{
				statusMessage_remote = "no arduino device detected";
				mCanvas.postInvalidate();
			}

	        if (serverSocket1 != null) {
	        	serverSocket1.close();
	        }
		}
		
		public boolean running = true;
		public void stopRun(){
			
		}
	}
	
	public class RemoteServerThread extends Thread{
		public RemoteServerThread(){
			
		}
		
		long prevTime = -1;
		@SuppressLint("NewApi") public void run(){
			startTrialTime = System.currentTimeMillis();
			try{
				serverSocket2 = new DatagramSocket(5000);
				
			}catch(Exception e){
				e.printStackTrace();
			}
			
			
			while(running){
				try{
					byte[] receiveData1 = new byte[1024];
					DatagramPacket receivePacket = new DatagramPacket(receiveData1, receiveData1.length);
					//statusMessage_UDP = "waiting...";
					
					serverSocket2.receive(receivePacket);
					msg2 += new String(receiveData1, 0, receivePacket.getLength());
					//statusMessage_UDP = "Sending!";
					
					String[] words = msg2.split(":");
					//Log.i("MSG_2", msg2);
					
					if (words.length > 9){
						for(int part = 1; part < words.length-1; part++){
							String[] cXYZ = words[part].split(",");
							int whatPart = getPart(cXYZ[0]);
							
							for(int i=1; i<cXYZ.length; i++){
								xyz2[whatPart][i-1] = Double.parseDouble(cXYZ[i]);
							}								
						}
						
						msg2 = "";
					}
					mCanvas.postInvalidate();
					
				}catch(Exception e){
					e.printStackTrace();
				}
			}
			
	        if (serverSocket2 != null) {
	        	serverSocket2.close();
	        }
		}
		
		public boolean running = true;
		public void stopRun(){
			
		}
	}
	

	final int LE=0, RE=1, LW=2, RW=3, LK=4, RK=5, LA=6, RA=7;
	public int getPart(String partID){
		int part = 0;
		
		if (partID.equals("LE")) return LE;
		else if (partID.equals("RE")) return RE;
		else if (partID.equals("LW")) return LW;
		else if (partID.equals("RW")) return RW;
		else if (partID.equals("LK")) return LK;
		else if (partID.equals("RK")) return RK;
		else if (partID.equals("LA")) return LA;
		else if (partID.equals("RA")) return RA;
		
		return part;
	}

	
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        //setContentView(R.layout.activity_main);
        mCanvas = new MyCanvas(this);
        setContentView(mCanvas);
        
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);

		mBluetoothAdapter = BluetoothAdapter.getDefaultAdapter();  
		Set<BluetoothDevice> pairedDevices = mBluetoothAdapter.getBondedDevices();
		// If there are paired devices
		if (pairedDevices.size() > 0) {
		    // Loop through paired devices
		    for (BluetoothDevice device : pairedDevices) {
		    	String deviceName = device.getName();
		    	if(deviceName.startsWith("REMOTE")) arduinoDev = device;
		    }
		}
		mCanvas.postInvalidate();
		
		
		rst = new RemoteClientThread();
        rst.start();
        
        rst2 = new RemoteServerThread();
        rst2.start();
    }

    int tc = 0;
    @Override
	public boolean dispatchTouchEvent(MotionEvent ev) {
		// TODO Auto-generated method stub
    	int action = ev.getAction();
    	switch(action){
    	case MotionEvent.ACTION_UP:
    		break;
    	}
		return super.dispatchTouchEvent(ev);
	}


	@Override
    public boolean onCreateOptionsMenu(Menu menu) {
        // Inflate the menu; this adds items to the action bar if it is present.
        getMenuInflater().inflate(R.menu.main, menu);
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        // Handle action bar item clicks here. The action bar will
        // automatically handle clicks on the Home/Up button, so long
        // as you specify a parent activity in AndroidManifest.xml.
        int id = item.getItemId();
        if (id == R.id.action_settings) {
            return true;
        }
        return super.onOptionsItemSelected(item);
    }
}
