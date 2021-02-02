#include <SoftwareSerial.h>

#define pnLedB    12
#define pnLedR    11
#define pnBuzz    10
#define pnBtn1    8
#define pnJrEcho  6
#define pnJrTrig  5

#define dangerDistance  60
#define saveDistance    150

// Untuk bluetooth
SoftwareSerial mySerial(2, 3); // TX , RX

int btnState = 0;
bool isAndroidMode = true;

void startLed()
{
  delay(200);
  digitalWrite(LED_BUILTIN, HIGH);
  digitalWrite(pnLedR, HIGH);
  delay(300);
  digitalWrite(LED_BUILTIN, LOW);
  digitalWrite(pnLedR, LOW);
  delay(50);
  digitalWrite(LED_BUILTIN, HIGH);
  digitalWrite(pnLedR, HIGH);
  delay(50);
  digitalWrite(LED_BUILTIN, LOW);
  digitalWrite(pnLedR, LOW);
  delay(50);
  digitalWrite(LED_BUILTIN, HIGH);
  digitalWrite(pnLedR, HIGH);
  delay(50);
  digitalWrite(LED_BUILTIN, LOW);
  digitalWrite(pnLedR, LOW);
  
  delay(100);
  digitalWrite(pnLedB, HIGH);
  delay(300);
  digitalWrite(pnLedB, LOW);
  delay(50);
  digitalWrite(pnLedB, HIGH);
  delay(50);
  digitalWrite(pnLedB, LOW);
  delay(50);
  digitalWrite(pnLedB, HIGH);
  delay(50);
  digitalWrite(pnLedB, LOW);
  
}

void dangerPosition()
{  
  digitalWrite(pnLedB, LOW);
  digitalWrite(LED_BUILTIN, LOW);
  digitalWrite(pnLedR, LOW);
  
  for (int l1 = 0; l1 < 2; ++l1)
  {
    digitalWrite(LED_BUILTIN, HIGH);
    digitalWrite(pnLedR, HIGH);
    for(int hz = 440; hz < 1000; hz+=4){
      tone(pnBuzz, hz, 50);
      delay(4);
    }
    noTone(pnBuzz);

    for(int hz = 1000; hz > 440; hz-=4){
      tone(pnBuzz, hz, 50);
      delay(4);
    }
    noTone(pnBuzz); 
    digitalWrite(LED_BUILTIN, LOW);
    digitalWrite(pnLedR, LOW);
    delay(50);
  }
  digitalWrite(LED_BUILTIN, HIGH);
  digitalWrite(pnLedR, HIGH);
}

void savePosition()
{  
  digitalWrite(pnLedB, LOW);
  digitalWrite(LED_BUILTIN, LOW);
  digitalWrite(pnLedR, LOW);

  for (int k = 0; k < 2; ++k)
  {    
    digitalWrite(pnLedB, HIGH);
  // Whoop up
    for(float hz = 1500; hz < 2500; hz=hz*1.05){
      tone(pnBuzz, hz, 20);
      delay(8);
    }
    noTone(pnBuzz);

    // Whoop down
    for(float hz = 5400; hz < 1500; hz=hz/1.05){
      tone(pnBuzz, hz, 25);
      delay(8);
    }
    noTone(pnBuzz); 
    digitalWrite(pnLedB, LOW);
    delay(50);
  }
  digitalWrite(pnLedB, HIGH);
}

void farPosition()
{  
  digitalWrite(pnLedB, LOW);
  digitalWrite(LED_BUILTIN, LOW);
  digitalWrite(pnLedR, LOW);
}


// using android or No Android
void switchMode()
{
  isAndroidMode = !isAndroidMode; //change flag variable
  if (isAndroidMode)
  {
    digitalWrite(pnLedB, HIGH);
    delay(300);
    digitalWrite(pnLedB, LOW);      
    delay(100);
    digitalWrite(pnLedB, HIGH);
    delay(50);
    digitalWrite(pnLedB, LOW);
    delay(50);
    digitalWrite(pnLedB, HIGH);
    delay(50);
    digitalWrite(pnLedB, LOW);  
  }    
  else
  {
    digitalWrite(pnLedB, HIGH);
    delay(300);
    digitalWrite(pnLedB, LOW);
    digitalWrite(pnLedR, HIGH);
    delay(100);
    digitalWrite(pnLedR, LOW);
    delay(50);
    digitalWrite(pnLedR, HIGH);
    delay(50);
    digitalWrite(pnLedR, LOW);
  }
  
  delay(250); //Small delay    
}

int calcDistance()  // in milimeter
{
  digitalWrite(pnJrTrig, LOW);
  delayMicroseconds(2);
  digitalWrite(pnJrTrig, HIGH);
  delayMicroseconds(10);
  digitalWrite(pnJrTrig, LOW);
  unsigned long duration = pulseIn(pnJrEcho, HIGH); 
  int distance = duration * 0.1711;
  return distance;
}

void evaluateDistace(int mmDistance)
{
  if (mmDistance < dangerDistance)
  {
    dangerPosition();
  }
  else if(mmDistance < saveDistance)
  {
    savePosition();
  }
  else
  {
    farPosition();
  }
}

// -------------------------------------------
// ARDUINO ...
// -------------------------------------------
void setup() 
{
  pinMode(LED_BUILTIN, OUTPUT);
  pinMode(pnLedB, OUTPUT);
  pinMode(pnLedB, OUTPUT);
  pinMode(pnBuzz, OUTPUT);  
  pinMode(pnBtn1, INPUT_PULLUP);  
  pinMode(pnJrTrig, OUTPUT); 
  pinMode(pnJrEcho, INPUT);

  mySerial.begin(9600);
  isAndroidMode = true;
  startLed();
  delay(100);
}

void loop() 
{
  btnState = digitalRead(pnBtn1);

  if (btnState == LOW) { 
    switchMode();   
  }

  if(isAndroidMode)
  {
    if(mySerial.available() > 0)
    {
      char c = mySerial.read();
      if (c == 'A')
      {
        mySerial.write("OK!\n");
      }
      else if (c == 'X')
      {
        int mmDistance = calcDistance();
        String noteS = ">" + String(mmDistance) + "<\n";
        char noteC[20];
        noteS.toCharArray(noteC, 20);
        mySerial.write(noteC);
        evaluateDistace(mmDistance);
      }
    }
  }
  else // not connect to Android device
  {
    int mmDistance = calcDistance();
    evaluateDistace(mmDistance);
    delay(500);
  }

}
