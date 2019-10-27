#include <ESP8266WiFi.h>
#include <ESP8266WebServer.h>
#include <WiFiUdp.h>
#include <LiquidCrystal_I2C.h>
#include <WiFiManager.h>

//General Init of vars
WiFiServer server(80);
const int button = 16;
WiFiUDP Udp;
unsigned int localUdpPort = 6666;
char incomingPacket[255];
long randNumber;
int lcdColumns = 20;
int lcdRows = 4;

//Iinit the LCD display
LiquidCrystal_I2C lcd(0x27, lcdColumns, lcdRows);

//This is I create the thermometer symbol.https://maxpromer.github.io/LCD-Character-Creator/ if you want an easy way to create custom symbols
byte thermom[8] = 
{
  B00100,
  B01010,
  B01010,
  B01110,
  B01110,
  B11111,
  B11111,
  B01110
};

byte degree[8] = {
  B11100,
  B10100,
  B11100,
  B00000,
  B00111,
  B00100,
  B00100,
  B00111
};
byte percent[8] = {
  B11001,
  B11011,
  B00111,
  B00110,
  B01100,
  B11100,
  B11011,
  B10011
};


//Function that will put the chip into wifi config mode if you either reset it or this is the first time the arduino is booted
void configModeCallback(WiFiManager *myWifiManager) {
  lcd.clear();
  lcd.setCursor(0,0);
  lcd.print("AP=PC Monitor");
  lcd.setCursor(0,1);
  lcd.print("URL=192.168.4.1");
  Serial.println("CONFIGMODE");
}


//Setup Function
void setup()
{

  //Configure the LCD display and then creates our custom symbols 
  lcd.init();
  lcd.begin(20,4);
  lcd.backlight();
  lcd.setCursor(0,0);
  lcd.clear();
  lcd.createChar(0,thermom);
  lcd.createChar(1,degree);


  //Debug functions
  Serial.begin(115200);
  Serial.print("ON");
  Serial.println();

  //Inits the button for resets
  pinMode(button, INPUT);
  //Inits the wifiManager which allows us to connect the Arduino to an access point
  WiFiManager wifiManager;
  //Displays text on the screen to instruct the user to press the button if they want to put the device into wifi configuration mode
  lcd.setCursor(0,0);
  lcd.print("Press button for");
  lcd.setCursor(0,1);
  lcd.print("Wifi setup");

  //Loop that will wait for 8 seconds to see if the user presses the button to put it into wifi config mode
  for (int i = 0; i < 4; i++) {
    Serial.println(digitalRead(button));
    if (!digitalRead(button)){
      lcd.clear();
      lcd.setCursor(0,0);
      lcd.print("AP=PC Monitor");
      lcd.setCursor(0,1);
      lcd.print("URL=192.168.4.1");
      Serial.println("BUTTON PRESSED");
      //If the button is pressed then it will reset the saved wifi ssid and password information
      wifiManager.resetSettings();
  
      break;
    }
    delay(2000);
  }
  
//In this function the Wifimanager attempts to connect to the saved SSID and password, if there is no saved information
//it will call the configModeCallback function to put it into Wifi config mode
wifiManager.setAPCallback(configModeCallback); 
//puts the device into wifi config mode with an ssid of PC Monitor 
wifiManager.autoConnect("PC Monitor");



  lcd.clear();
  lcd.print("Connected to");
  lcd.setCursor(0,1);
  lcd.print("Wifi");
  delay(4000);
  Serial.print("Connected, IP address: ");
  Serial.println(WiFi.localIP());
  lcd.clear();
  Udp.begin(localUdpPort);
  Serial.printf("Now listening at IP %s, UDP port %d\n", WiFi.localIP().toString().c_str(), localUdpPort);
  
  //Init the base information for the LCD screen for CPU
  lcd.setCursor(0,0);
  lcd.print("CPU       U:   %");
  lcd.setCursor(4,0);
  lcd.print((char)0);
  lcd.setCursor(8,0);
  lcd.print((char)1);
  
  //GPU Information Init
  lcd.setCursor(0,1);
  lcd.print("GPU       U:   %");
  lcd.setCursor(4,1);
  lcd.print((char)0);
  lcd.setCursor(8,1);
  lcd.print((char)1);
  //Mem Information Init
  lcd.setCursor(0,2);
  lcd.print("MEM     GB/   GB");

  
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
    String gput = String(incomingPacket[6]) + String(incomingPacket[7]) + String(incomingPacket[8]);
    String gpuu = String(incomingPacket[9]) + String(incomingPacket[10]) + String(incomingPacket[11]);
    String used_mem = String(incomingPacket[12]) + String(incomingPacket[13]) + String(incomingPacket[14]);
    String total_mem = String(incomingPacket[15]) + String(incomingPacket[16]) + String(incomingPacket[17]);
    Serial.println("CPUT: " + cput);
    Serial.println("CPUU: " + cpuu);
    Serial.println("GPUT: " + gput);
    Serial.println("GPUU: " + gpuu);
    
    
    Serial.printf("UDP Packet Contents: %s\n", incomingPacket);
    Udp.beginPacket('10.0.0.255', '6666');
    //Udp.write(replyPacket);
    Udp.endPacket();
    //Write CPU Info to the screen
    lcd.setCursor(5,0);
    lcd.print(String(cput));
    lcd.setCursor(12,0);
    lcd.print(String(cpuu));
    //Write GPU Info to the screen
    lcd.setCursor(5,1);
    lcd.print(String(gput));
    lcd.setCursor(12,1);
    lcd.print(String(gpuu));
    //Write RAM info to the screen
    lcd.setCursor(5,2);
    lcd.print(String(used_mem));
    lcd.setCursor(11,2);
    lcd.print(String(total_mem));
    
    Serial.printf("SENT PACKET");
  } 
  
}
