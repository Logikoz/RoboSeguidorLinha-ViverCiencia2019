//Bibliotecas
#include <SPI.h>
#include <MFRC522.h>

//Pinos
#define BUZZER 7
#define LED_VERDE 6
#define LED_VERMELHO 5
#define SS_PIN 10
#define RST_PIN 9

String IDtag = ""; //Variável que armazenará o ID da Tag
String CartaoParada = "CARD_STOP_AQUI";
String CartoesPositivos[] = { "CARD_P", "CARD_P" };
String CartoesNegativos[] = { "CARD_N", "CARD_N", "CARD_N" };

MFRC522 LeitorRFID(SS_PIN, RST_PIN);    // Cria uma nova instância para o leitor e passa os pinos como parâmetro


void setup() {
  Serial.begin(115200);             // Inicializa a comunicação Serial
  SPI.begin();                      // Inicializa comunicacao SPI 
  LeitorRFID.PCD_Init();            // Inicializa o leitor RFID
  pinMode(LED_VERDE, OUTPUT);       // Declara o pino do led verde como saída
  pinMode(LED_VERMELHO, OUTPUT);    // Declara o pino do led vermelho como saída
  pinMode(BUZZER, OUTPUT);          // Declara o pino do buzzer como saída
}

void loop() {  
  Leitura();  //Chama a função responsável por fazer a leitura das Tag's
}

void Leitura()
{
        IDtag = ""; //Inicialmente IDtag deve estar vazia.
        
        // Verifica se existe uma Tag presente
        if ( !LeitorRFID.PICC_IsNewCardPresent() || !LeitorRFID.PICC_ReadCardSerial() ) {
            delay(50);
            return;
        }
        
        // Pega o ID da Tag através da função LeitorRFID.uid e Armazena o ID na variável IDtag        
        for (byte i = 0; i < LeitorRFID.uid.size; i++) {        
            IDtag.concat(String(LeitorRFID.uid.uidByte[i], HEX));
        }

        acessoLiberado();

        delay(5000); //aguarda 5 segundos para efetuar uma nova leitura
}

void acessoLiberado()
{
  Serial.print(IDtag); //Exibe a mensagem "Tag Cadastrada" e o ID da tag não cadastrada

  if(IDtag != CartaoParada){
  efeitoPermitido();  //Chama a função efeitoPermitido().
  CartaoPositivo();
  CartaoNegativo();
  Desligar();
  }
}

void Desligar(){
  if(IDtag == CartaoParada){
    digitalWrite(LED_VERDE, LOW);
    digitalWrite(LED_VERMELHO, LOW);
  }
}

void CartaoPositivo(){
  for(int i = 0; i < 2; i++){
    if(CartoesPositivos[i] == IDtag)
    {
      digitalWrite(LED_VERMELHO, LOW);
      digitalWrite(LED_VERDE, HIGH); 
    }
  }
}

void CartaoNegativo(){
  for(int i = 0; i < 3; i++){
    if(CartoesNegativos[i] == IDtag)
    {
      digitalWrite(LED_VERDE, LOW);
      digitalWrite(LED_VERMELHO, HIGH); 
    }
  }
}

void efeitoPermitido(){  
  int qtd_bips = 1; //definindo a quantidade de bips
  for(int j=0; j<qtd_bips; j++){
    //Ligando o buzzer com uma frequência de 1500 hz e ligando o led verde.
    tone(BUZZER,1500);
    delay(100);   
    
    //Desligando o buzzer e led verde.      
    noTone(BUZZER);
    delay(100);
  }  
}
