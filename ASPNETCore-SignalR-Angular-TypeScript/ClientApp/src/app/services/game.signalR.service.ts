import { EventEmitter, Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, IStreamResult } from '@aspnet/signalr'
import { Vehicle } from '../Models/Vehicle.model'


@Injectable()
export class gameSignalRService {
  connectionEstablished = new EventEmitter<Boolean>();
  gameOpened = new EventEmitter<Boolean>();
  gameClosed = new EventEmitter<Boolean>();
  gameReset = new EventEmitter<Boolean>();
  gameLoopBenchmark = new EventEmitter<number>();
  gameLoopVehicles = new EventEmitter<any>();

  private connectionIsEstablished = false;
  private _gameHubConnection: HubConnection;


  constructor() {
    this.createConnection();
    this.registerOnServerEvents();
    this.startConnection();
  }

  private createConnection() {
    this._gameHubConnection = new HubConnectionBuilder()
      .withUrl('/game')
      .build();
  }

  private startConnection(): void {
    this._gameHubConnection
      .start()
      .then(() => {
        this.connectionIsEstablished = true;
        console.log('game connection started');
        this.connectionEstablished.emit(true);
      }).catch(err => {
        console.log('wth happened ' + err);
        setTimeout(this.startConnection(), 5000);
      });
  }

  private registerOnServerEvents(): void {
    this._gameHubConnection.on("gameOpened", () => {
      console.log("gameOpened");
      this.gameOpened.emit(true);
    });

    this._gameHubConnection.on("gameClosed",() => {
      console.log("gameClosed");
      this.gameClosed.emit(true);
    });

    this._gameHubConnection.on("gameReset",() => {
      console.log("gameReset");
      this.gameReset.emit(true);
    });

    this._gameHubConnection.on("gameLoopBenchmark",(gameLoopInMilliseconds:number) => {
      this.gameLoopBenchmark.emit(gameLoopInMilliseconds);
    });

    this._gameHubConnection.on("gameLoopVehicles", (gameLoopVehicles: any) => {
      console.log('received vehicles from game loop: ')
      console.log(gameLoopVehicles);
      this.gameLoopVehicles.emit(gameLoopVehicles);
    });
  }

  public startStreaming(): IStreamResult<any> {
    return this._gameHubConnection.stream("StreamVehicles");
  }

  public getAllVehicles(): Promise<any> {
    return this._gameHubConnection.invoke("getAllVehicles");
  }

  public openGame() {
    this._gameHubConnection.invoke("OpenGame");
  }

  public closeGame() {
    this._gameHubConnection.invoke("CloseGame");
  }

  public resetGame() {
    this._gameHubConnection.invoke("Reset");
  }

  public toggleAdaptiveCruise(){
    this._gameHubConnection.invoke("ToggleAdaptiveCruise");
  }
  
  public turnRight() {
    this._gameHubConnection.invoke("TurnRight");
  }

  public turnLeft() {
    this._gameHubConnection.invoke("TurnLeft");
  }

  public increaseSpeed() {
    this._gameHubConnection.invoke("IncreaseSpeed");
  }

  public decreaseSpeed() {
    this._gameHubConnection.invoke("DecreaseSpeed");
  }
}
