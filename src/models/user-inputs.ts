export enum FormationShapes {
    Line = 0,
    Square = 1,
    Box = 2,
    Triangle = 3,
    V = 4,
    Circle = 5
}

export interface Point {
    X: number;
    Y: number;
}

export interface WaypointGrid {
    Position: Point;
    Heading: number;
    MaxSpeed: number;
}

export interface FormationShapeDto {
    Shape: FormationShapes;
    Size: number;
}

export interface FormationPathDto {
    Waypoints: WaypointGrid[];
    ModeChanged: boolean;
}

export interface VehicleTargetsDto {
    Waypoints: WaypointGrid[];
    AgentId: number;
    OverRide: boolean;
    ModeChanged: boolean;
}
