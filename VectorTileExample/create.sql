create table points (id serial, name varchar(100), location geometry, primary key (id));
insert into points (name, location) values 
    ('東京駅', st_geomfromtext('POINT(139.76769903733918 35.68139335470638)', 4326)),
    ('東京タワー', st_geomfromtext('POINT(139.74540410167575 35.658607404547354)', 4326)),
    ('東京スカイツリー', st_geomfromtext('POINT(139.81069924696237 35.710000524374095)', 4326))