export class Vehicle{
    name:string;
    mph:number;
    x:number;
    y:number;
    adaptiveCruiseOn:boolean;
    adaptiveCruiseMph:number;
    status:string;
    drivingStatus:string;
    drivingAdjective:string;
    points:number;
    adaptiveCruiseFrontRadarIndicator:Boolean;
    leftBlindSpotIndicator:boolean;
    rightBlindSpotIndicator: boolean;
    isHazard: boolean;
    constructor(){
        this.mph = 0;
        this.x = 0;
        this.y = 0;
        this.adaptiveCruiseFrontRadarIndicator = false;
        this.adaptiveCruiseOn = false;
        this.adaptiveCruiseMph = 0;
        this.status = "";
        this.drivingStatus = "";
        this.drivingAdjective = "";
    }
}
