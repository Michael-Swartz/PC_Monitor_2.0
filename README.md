# PC_Monitor_2.0

Simple device I created to monitor my PCs performance via an ESP8266. The data is pulled via a custom windows service using the Openhardwaremonitor.dll. It brodcasts this data at a predetermined interval to a specific port on my network which is captured via a socket listning on that port on the ESP8266. The user interface baked into the Arduino code is mainly based on the Wifimanager library.

