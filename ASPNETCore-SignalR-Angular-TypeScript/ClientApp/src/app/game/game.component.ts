import { Component, HostListener } from "@angular/core";

import { gameSignalRService } from "../services/game.signalR.service";
import { forEach } from "@angular/router/src/utils/collection";
import { Vehicle } from "../Models/Vehicle.model";
import { Cell, HighwayRow } from "../Models/cell.model"

export enum KEY_CODE {
  RIGHT_ARROW = 39,
  LEFT_ARROW = 37,
  DOWN_ARROW = 40,
  UP_ARROW = 38
}

@Component({
  templateUrl: './game.component.html',
  styleUrls: ['./game.styles.css'],
  selector:"app-game"
})

export class GameComponent {

  vehicles = []; // this gets updated at the completion of a game loop and rendered to ui
  vehiclesStream = []; // gets updated during game cycle
  player1Vehicle:Vehicle;
  gameStatus: string = "reset";
  gameLevel: string = "Level1";
  public lanes = [1,2,3,4,5,6,7,8];
  public rows = [];
  public highwayRows:HighwayRow[] = [];
  public alert:string;
  readonly showNumberOfCellsAhead:number = 14;
  readonly showNumberOfCellsBehind:number = 14;
  readonly PLAYER1:string = "Player 1";
  public gameLoopInMilliseconds = 0;

  constructor(private gameService: gameSignalRService) {
    this.player1Vehicle = new Vehicle();
    this.vehiclesStream = [];
    this.vehicles = [];

    //subscribe for connection establish
    //fetch the vehicles details
    gameService.connectionEstablished.subscribe(() => {
      this.resetGame();
    });

    //subscribe for game open event
    gameService.gameOpened.subscribe(() => {
      this.gameStatus = 'open';
      this.startStreaming();
    });

    //subscribe for game close event
    gameService.gameClosed.subscribe(() => {
      this.gameStatus = 'closed';
      this.alert = "Game over. You got " + this.player1Vehicle.points + " points!";
    });

    //subscribe for game reset event
    gameService.gameReset.subscribe(() => {
      this.gameStatus = "reset";
      this.alert = "Game reset.";
      this.initVehicles();
    });

    //subscribe for game won event
    gameService.gameWon.subscribe(() => {
      alert('You won the game and got the max points, ' + this.player1Vehicle.points + '!!! You desserve a new car!!');
    });

    //subscribe for game loss event
    gameService.gameLost.subscribe(() => {
      alert('Game over, you got ' + this.player1Vehicle.points + ' points. Keep practicing to get max points!');
    });

    //subscribe for game loop events
    gameService.gameLoopBenchmark.subscribe((gameLoopInMilliseconds: number) => {
      this.gameLoopInMilliseconds = gameLoopInMilliseconds;
    });
    gameService.gameLoopVehicles.subscribe((gameLoopVehicles: any) => {
      let gameLoopVehiclesArray: Array<Vehicle> = gameLoopVehicles as Array<Vehicle>;
      for (var i = 0, len = gameLoopVehiclesArray.length; i < len; i++) {
        this.displayVehicle(gameLoopVehiclesArray[i]);
      }
      this.renderHwy();
    });
  }

  initVehicles() {
    this.gameService.getAllVehicles().then((data) => {
      this.vehicles = data;
      this.vehiclesStream = data;
      this.renderHwy();
    });
  }

  startStreaming() {
    this.gameService.startStreaming().subscribe({
      next: (data) => {
        this.displayVehicleStream(data);
      },
      error: function (err) {
        console.log('Error:' + err);
      },
      complete: function () {
        console.log('completed');
      }
    });
  }

  displayVehicle(vehicle) {
    //console.log("vehicle updated:" + vehicle.name);
    for (let i in this.vehicles) {
      if (this.vehicles[i].name == vehicle.name) {
        this.vehicles[i] = vehicle;
      }
      if (vehicle.name === "Player 1") {
        this.player1Vehicle = vehicle;
      }
    }
  }

  displayVehicleStream(vehicle) {
    //console.log("vehicle updated:" + vehicle.name);
    for (let i in this.vehiclesStream) {
      if (this.vehicles[i].name == vehicle.name && vehicle[i].isHazard == false) {
        this.vehiclesStream[i] = vehicle;
      }
    }
  }

  openGameClicked() {
    this.gameService.openGame();
  }
  closeGameClicked() {
    this.gameService.closeGame();
  }

  resetClicked() {
    this.resetGame();
  }
  resetGame() {
    this.alert = "";
    this.rows = [];
    this.renderHwy();
    this.gameService.resetGame(this.gameLevel);
  }

  playAgainClicked() {
    this.alert = "";
    this.rows = [];
    this.gameService.resetGame(this.gameLevel);
  }


  @HostListener('window:keydown', ['$event'])
  keyEvent(event: KeyboardEvent) {   
    if (event.keyCode === KEY_CODE.RIGHT_ARROW) {
      this.turnRight();
      event.preventDefault();
    }

    if (event.keyCode === KEY_CODE.LEFT_ARROW) {
      this.turnLeft();
      event.preventDefault();
    }

    if (event.keyCode === KEY_CODE.UP_ARROW) {
      this.increaseSpeed();
      event.preventDefault();
    }

    if (event.keyCode === KEY_CODE.DOWN_ARROW) {
      this.decreaseSpeed();
      event.preventDefault();
    }
  }
  
  turnRight() {
    this.gameService.turnRight();
  }
  
  turnLeft() {
    this.gameService.turnLeft();
  }

  increaseSpeed() {
    this.gameService.increaseSpeed();
  }

  decreaseSpeed() {
    this.gameService.decreaseSpeed();
  }

  toggleAdaptiveCruise(){
    this.gameService.toggleAdaptiveCruise();
  }
  levelClick(level:number) {
    this.gameLevel = "Level" + level;
  }

  // ***** ALL RENDERING ***********
  renderHwy(){
    // clear rows
    this.rows = []; 
    this.highwayRows = [];
    var lowEnd = this.player1Vehicle.x - this.showNumberOfCellsBehind;
    var highEnd = this.player1Vehicle.x + this.showNumberOfCellsAhead;
    for (var n = highEnd; n >= lowEnd; n--) {
        this.rows.push(n);
    }
    for(let row of this.rows){
      var hwyRow = new HighwayRow();
      hwyRow.Lane1 = this.renderCell(row, 1);
      hwyRow.Lane2 = this.renderCell(row, 2);
      hwyRow.Lane3 = this.renderCell(row, 3);
      hwyRow.Lane4 = this.renderCell(row, 4);
      hwyRow.Lane5 = this.renderCell(row, 5);
      hwyRow.Lane6 = this.renderCell(row, 6);
      hwyRow.Lane7 = this.renderCell(row, 7);
      hwyRow.Lane8 = this.renderCell(row, 8); 
      this.highwayRows.push(hwyRow);
    }
  }
  renderCellMarker(cell:Cell, x:number, y:number):Cell{
    cell.marker = x;
    return cell;
  }
  renderCellLine(cell:Cell, y:number):Cell{
    if(y === 2 || y === 8){
      cell.line = "|";
    }
    return cell;
  }
  renderCellVehicle0Plus3(cell:Cell, x:number, y:number, vehicle:Vehicle){
    // scan the leading cell in front of each vehicle
    for (let vehicle of this.vehicles) {
      if(vehicle.y == y 
        && x == vehicle.x+3 
        && vehicle.adaptiveCruiseOn
        && vehicle.adaptiveCruiseFrontRadarIndicator){
        cell.content = '=====';
      }
    }
    return cell;
  }
  renderCellVehicle0Plus2(cell:Cell, x:number, y:number, vehicle:Vehicle):Cell{
    if(x == (vehicle.x+2)){
      // first row of vehicle
      cell.content += "(" + vehicle.mph.toLocaleString() + " mph) ";// clear any lines
    }
    return cell;
  }
  renderCellVehiclePlus1(cell:Cell, x:number, y:number, vehicle:Vehicle):Cell{
    if(x == (vehicle.x+1)){
      if(vehicle.adaptiveCruiseOn){
        cell.content += "Cruise On: (" + vehicle.adaptiveCruiseMph + " mph)";
      }
      else{
        cell.content += "Cruise Off";
      }
    }
    return cell;
  }
  renderCellVehicle0(cell:Cell, x:number, y:number, vehicle:Vehicle):Cell{
    if(x == (vehicle.x)){
      // third row of vehicle
      if(cell.vehicle.leftBlindSpotIndicator){
        cell.content += "<*";
      }
      else {
        cell.content += "<";
      }
      cell.content += vehicle.name;
      if(cell.vehicle.rightBlindSpotIndicator){
        cell.content += "*>";
      }
      else {
        cell.content += ">";
      }
    }
    return cell;
  }
  renderCellVehicleMinus1(cell:Cell, x:number, y:number, vehicle:Vehicle):Cell{
    if(x == (vehicle.x-1)){
      if(x == (vehicle.x+1)){
        cell.content += vehicle.drivingAdjective;
      }
    }
    return cell;
  }
  renderCellVehicleMinus2(cell:Cell, x:number, y:number, vehicle:Vehicle):Cell{
    if(x == (vehicle.x-2)){
      cell.content += vehicle.name;
    }
    return cell;
  }
  renderCellVehicleMinus3(cell:Cell, x:number, y:number, vehicle:Vehicle):Cell{
    if(x == (vehicle.x - 3) && vehicle.drivingStatus != ""){
      // last cell of vehicle
      cell.content = "[" + vehicle.drivingStatus + "]";
    }
    return cell;
  }
  renderCell(x:number, y:number): Cell {
    let cell:Cell = new Cell(x, y);

    cell = this.renderCellMarker(cell, x, y);
    cell = this.renderCellLine(cell, y);
    cell = this.renderCellPaintedLine(cell);

    for (let vehicle of this.vehicles) 
    {
      if(vehicle.y == y
        && vehicle.x <= (x+3)
        && vehicle.x >= (x-3))
      {
        cell.vehicle = vehicle;
        cell = this.renderCellVehicle0Plus3(cell, x, y, vehicle);
        cell = this.renderCellVehicle0Plus2(cell, x, y, vehicle);
        cell = this.renderCellVehiclePlus1(cell, x, y, vehicle);
        cell = this.renderCellVehicle0(cell, x, y, vehicle);
        cell = this.renderCellVehicleMinus1(cell, x, y, vehicle);
        cell = this.renderCellVehicleMinus2(cell, x, y, vehicle);
        cell = this.renderCellVehicleMinus3(cell, x, y, vehicle);
        break;
      }
    }
    return cell;
  }

  renderCellPaintedLine(cell:Cell): Cell{
    var s = cell.marker.toLocaleString();
    var lastChar = s.substr(s.length - 1);
    if(cell.lane === 4 || cell.lane == 6){
      if(lastChar === "2" || lastChar === "3" || lastChar === "4" || lastChar === "5"){
        cell.line = "|";
      }
    }
    return cell;
  }

  otherVehiclesInLaneAheadOfPlayer1(lane:number, inDrivingStatus:string, exDrivingStatus:string):number{
    var count:number = 0;
    if(inDrivingStatus.length > 0){
      for (let vehicle of this.vehicles) {
        if(vehicle.y == lane
          && vehicle.name != this.PLAYER1
          && !vehicle.adaptiveCruiseOn
          && vehicle.drivingStatus === inDrivingStatus
          && vehicle.x >= this.player1Vehicle.x)
        {
          count++;
        }
      }
    }
    else{
      for (let vehicle of this.vehicles) {
        if(vehicle.y == lane
          && vehicle.name != this.PLAYER1
          && !vehicle.adaptiveCruiseOn
          && vehicle.drivingStatus !== exDrivingStatus
          && vehicle.x >= this.player1Vehicle.x)
        {
          count++;
        }
      }
    }
    return count;
  }

  otherVehiclesInLaneBehindPlayer1(lane:number, inDrivingStatus:string, exDrivingStatus:string):number{
    var count:number = 0;
    
    if(inDrivingStatus.length > 0){
      for (let vehicle of this.vehicles) {
        if(vehicle.y == lane
          && vehicle.name != this.PLAYER1
          && !vehicle.adaptiveCruiseOn
          && vehicle.drivingStatus === inDrivingStatus
          && vehicle.x < this.player1Vehicle.x)
        {
          count++;
        }
      }
    }
    else{
      for (let vehicle of this.vehicles) {
        if(vehicle.y == lane
          && vehicle.name != this.PLAYER1
          && !vehicle.adaptiveCruiseOn
          && vehicle.drivingStatus !== exDrivingStatus
          && vehicle.x < this.player1Vehicle.x)
        {
          count++;
        }
      }
    }
    return count;
  }
}
