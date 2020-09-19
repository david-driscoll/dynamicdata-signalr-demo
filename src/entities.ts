export interface Hero {
    id: string;
    leaderId?: string | undefined;
    superHeroApiId?: number;
    name: string;
    realName: string;
    alignment: string;
    gender: string;
    race: string;
    publisher: string;
    powerstats: Powerstats;
    teams: string[];
    avatarUrl: string;
    created: string;
    updated?: string;
}

export interface SuperHeroRecord {
    response: string;
    id: string;
    name: string;
    powerstats: Powerstats;
    biography: Biography;
    appearance: Appearance;
    work: Work;
    connections: Connections;
    image: Image;
}

export interface Appearance {
    gender: string;
    race: string;
    height: string[];
    weight: string[];
    'eye-color': string;
    'hair-color': string;
}

export interface Biography {
    'full-name': string;
    'alter-egos': string;
    aliases: string[];
    'place-of-birth': string;
    'first-appearance': string;
    publisher: string;
    alignment: string;
}

export interface Connections {
    'group-affiliation': string;
    relatives: string;
}

export interface Image {
    url: string;
}

export interface Powerstats {
    intelligence?: string;
    strength?: string;
    speed?: string;
    durability?: string;
    power?: string;
    combat?: string;
}

export interface Work {
    occupation: string;
    base: string;
}
