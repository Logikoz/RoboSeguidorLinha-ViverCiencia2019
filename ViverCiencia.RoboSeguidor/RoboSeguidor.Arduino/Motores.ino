// --- Bibliotecas Auxiliares ---
#include <AFMotor.h> //Inclui biblioteca AF Motor

// --- Seleção dos Motores ---
AF_DCMotor motor1(2); //Motor da Direita
AF_DCMotor motor2(1); //Motor da Esquerda
//Sensores de Refletância
int sensorLinhaD = A14; //Sensor de Refletância da direita ligado na porta A15
int sensorLinhaE = A13; //Sensor de Refletância da esquerda ligado na porta A14
int valorLinhaE, valorLinhaD = 0; //variável que armazena o valor do sensor
//calibração dos sensores
int ValorCorte_E = 150; //se o valor for menor do que 150, o valor lido é linha branca, senão estará no preto (Ajustado previamente)
int ValorCorte_D = 150; //se o valor for menor do que 150, o valor lido é linha branca, senão estará no preto (Ajustado previamente)
//Contem o Cartao de parada que fica no portal.
String CartaoParada = "CARD_STOP";
bool Continuar = true;

//=====================================================================
void setup() {
  //Para o Shied a biblioteca se encarrega de setar entradas e saídas!
  //Demais pinos devem ser declarados
  Serial1.begin(115200);
  pinMode(trigPin, OUTPUT);
  pinMode(echoPin, INPUT);
  pinMode(sensorLinhaD, INPUT);
  pinMode(sensorLinhaE, INPUT);
}
void loop() {
  if(Continuar == false) 
  {
    motor_dir_parar();
    motor_esq_parar();
    return; 
  }
  
  if(Serial1.available())
  {
    motor_dir_parar();
    motor_esq_parar();
    if(Serial1.readString() == CartaoParada)
    {
      Continuar = false;
    }
    delay(3500);
  }
  
  //ler_sensor_utrassonico();
  //Leitura dos sensores de linha
  valorLinhaE = analogRead(sensorLinhaE);
  valorLinhaD = analogRead(sensorLinhaD);
  if ((valorLinhaE < ValorCorte_E) && (valorLinhaD > ValorCorte_D)) {
    motor_dir_tras();
    motor_esq_frente();
  }
  else if ((valorLinhaE > ValorCorte_E) && (valorLinhaD < ValorCorte_D)) {
    motor_dir_frente();
    motor_esq_tras();
  }
  else {
    motor_dir_frente();
    motor_esq_frente();
  }
}
//OBS também é possível criar funções para os motores
//conforme exemplo abaixo.
//motor da direita
void motor_dir_frente ()
{
  motor1.setSpeed(200);
  motor1.run(FORWARD);//
}
void motor_dir_tras ()
{
  motor1.setSpeed(220);
  motor1.run(BACKWARD);
}
void motor_dir_parar () {
  motor1.setSpeed(0);
  motor1.run(RELEASE);
}
//motor da esquerda
void motor_esq_frente () {
  motor2.setSpeed(150);
  motor2.run(FORWARD);
}
void motor_esq_tras () {
  motor2.setSpeed(220);
  motor2.run(BACKWARD);
}
void motor_esq_parar () {
  motor2.setSpeed(0);
  motor2.run(RELEASE);
}
