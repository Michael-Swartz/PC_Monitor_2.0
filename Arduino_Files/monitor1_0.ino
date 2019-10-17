#include <ESP8266WiFi.h>
#include <ESP8266WebServer.h>
#include <WiFiUdp.h>
#include <LiquidCrystal_I2C.h>
#include <WiFiManager.h>


WiFiServer server(80);
const int button = 16;
WiFiUDP Udp;
unsigned int localUdpPort = 6666;
char incomingPacket[255];
char replyPacket[] = "Hi i got your shit"; 
long randNumber;
int lcdColumns = 16;
int lcdRows = 2;

LiquidCrystal_I2C lcd(0x27, lcdColumns, lcdRows);

void configModeCallback(WiFiManager *myWifiManager) {
  lcd.clear();
  lcd.setCursor(0,0);
  lcd.print("AP=PC Monitor");
  lcd.setCursor(0,1);
  lcd.print("URL=192.168.4.1");
  Serial.println("CONFIGMODE");
}

void setup()
{
  
  bool fresh_boot = false; 
  lcd.init();
  lcd.backlight();
  lcd.setCursor(0,0);
  Serial.begin(115200);
  Serial.print("ON");
  Serial.println();
  pinMode(button, INPUT);
  WiFiManager wifiManager;
  
  lcd.print("Press button for");
  lcd.setCursor(0,1);
  lcd.print("Wifi mode");
  for (int i = 0; i < 4; i++) {
    Serial.println(digitalRead(button));
    if (!digitalRead(button)){
      lcd.clear();
      lcd.setCursor(0,0);
      lcd.print("AP=PC Monitor");
      lcd.setCursor(0,1);
      lcd.print("URL=192.168.4.1");
      Serial.println("BUTTON PRESSED");
      wifiManager.resetSettings();
  
      break;
    }
    delay(2000);
  }
  

wifiManager.setAPCallback(configModeCallback);  
wifiManager.autoConnect("PC Monitor");



  lcd.clear();
  lcd.print("Connected to");
  lcd.setCursor(0,1);
  lcd.print("Wifi");
  delay(4000);
  //WiFi.begin(ssid, pass);
//  lcd.print("Conecting to: " + String(ssid));
//  Serial.print("Connecting");
//  while (WiFi.status() != WL_CONNECTED)
//  {
//    delay(500);
//    Serial.print(".");
//  }
//  Serial.println();

  Serial.print("Connected, IP address: ");
  Serial.println(WiFi.localIP());
  lcd.clear();
  Udp.begin(localUdpPort);
  Serial.printf("Now listening at IP %s, UDP port %d\n", WiFi.localIP().toString().c_str(), localUdpPort);
  lcd.setCursor(0,0);
  lcd.print("CPU T:    U:");
  lcd.setCursor(0,1);
  lcd.print("GPU T:");
}


void loop() 
{

  int packetSize = Udp.parsePacket();
  if (packetSize)

  {
    Serial.printf("Received %d bytes from %s, port %d\n", packetSize, Udp.remoteIP().toString().c_str(), Udp.remotePort());
    int len = Udp.read(incomingPacket, 255);
    if (len > 0) 
    {
      incomingPacket[len] = '\0'; 
    }

    Serial.printf("Receieved: %s\n", incomingPacket);
    for(int i = 0; i < 5 ; i++) {
      Serial.println(incomingPacket[i]);   
    }
    
    String cput = String(incomingPacket[0]) + String(incomingPacket[1]) + String(incomingPacket[2]);
    String cpuu = String(incomingPacket[3]) + String(incomingPacket[4]) + String(incomingPacket[5]);
    Serial.println("CPUT: " + cput);
    
    
    Serial.printf("UDP Packet Contents: %s\n", incomingPacket);
    Udp.beginPacket('10.0.0.255', '6666');
    Udp.write(replyPacket);
    Udp.endPacket();
    lcd.setCursor(6,0);
    lcd.print(String(cput));
    lcd.setCursor(12,0);
    lcd.print(String(cpuu));
    Serial.printf("SENT PACKET");
  } 
  
}
