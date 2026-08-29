-- Rollback seed: seed_setup_work_centers_default

USE mtm_waitlist;

DELETE FROM setup_work_centers_catalog
WHERE
    work_center_name IN (
        '099-03 - Press (Enerpac)',
        '099-04 - Press (Enerpac)',
        '100-01 - Press Room 400 Ton Williams & White',
        '100-02 - Press Room 200 Ton Bliss',
        '100-03 - Press Room 220 Ton Aida',
        '100-04 - Press Room 60 Ton',
        '100-06 - Press Room 500 Ton Heim',
        '100-07 - Brake Press',
        '100-08 - Press Room 800 Ton',
        '100-09-Cell - Press Room 800 Ton & Studweld',
        '100-10 - Press Room 200 Ton Clearing',
        '100-12 - Press Room 440 Ton Seyi',
        '100-13 - Press Room Lien Chieh',
        '100-14 - Press Room 660 Ton Seyi',
        '100-18 - Press Room 330 Ton Seyi Servo'
    );