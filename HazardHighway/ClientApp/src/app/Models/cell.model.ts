import { Vehicle } from "./Vehicle.model";

export class Cell{
    row:number;
    lane:number;
    marker:number;
    line:string;
    content:string; // contents
    vehicle:Vehicle;
    constructor(row:number, lane:number){
        this.row = row;
        this.marker = 0;
        this.line = "";
        this.lane = lane;
        this.content = "";
        this.vehicle = null;
    }
    displaySafe():boolean{
        return !this.displayCrashed() 
            && this.vehicle!= null 
            && this.row <= this.vehicle.x+2 // exclude leading cell with radar indicator
            && this.row >= this.vehicle.x-2 // exclude trailing cell with status indicator
            && this.vehicle.adaptiveCruiseOn;
    }
    displayUnsafe():boolean{
        return !this.displayCrashed() 
            && this.vehicle!= null 
            && this.row <= this.vehicle.x+2 // exclude leading cell with radar indicator
            && this.row >= this.vehicle.x-2 // exclude trailing cell with status indicator
            && !this.vehicle.adaptiveCruiseOn;
    }
    displayCrashed():boolean{
        return this.vehicle!= null && this.vehicle.drivingStatus === 'Crashed';
    }
    show(){
        if(this.vehicle != null){
            return this.content;
        }
        else if (this.line.length > 0){
            return this.line;
        }
        else{
            return " ";
        }
    }
}

export class HighwayRow{
    Lane1:Cell;
    Lane2:Cell;
    Lane3:Cell;
    Lane4:Cell;
    Lane5:Cell;
    Lane6:Cell;
    Lane7:Cell;
    Lane8:Cell;
}