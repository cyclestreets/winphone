<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/index.php $
 * $Revision: 8 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-02-05 20:19:13 +0000 (Tue, 05 Feb 2008) $
 *********************************************************/
 
class uploaderWrapper
{
    var $uploader;

    function uploaderWrapper()
    {
        global $IN;

        require_once(ROOT_PATH."/engine/upload.php");
        $this->uploader = new CUpload();

        switch($IN["ACT"])
        {
            case 'uploadfile':
            $this->uploadfile();
            break;
        }
    }

    function uploadfile()
    {
        //uploadFile
    }
}

$loader = new uploaderWrapper();

?>
