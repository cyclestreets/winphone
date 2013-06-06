<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/captcha.php $
 * $Revision: 50 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-12-20 20:32:52 +0000 (Sat, 20 Dec 2008) $
 *********************************************************/

class captcha
{
    var $html;
    
    function captcha()
    {
        global $IN;
        
        switch($IN["ACT"])
        {
            case 'regbotimg':
            {
                $this->showRegBotImg();
                break;
            }
        }
    }
    
    function createImage()
    {
        global $DB, $IN;
        // First clear the database of old rows older than 5 hours
        $oldTime = time() - (18000);
        $DB->query("DELETE FROM dl_regbot WHERE regtime < '$oldTime'");

        // Generate new key
        mt_srand ((double) microtime() * 1000000);
        $reg_code = mt_rand(100000,999999);

        $insert = array("regKey" => $reg_code,
                        "ip" => $IN["ipaddr"],
                        "regtime" => time());
        $DB->insert($insert, "dl_regbot");
        $return = $DB->insert_id();
        return $return;
    }
    
    function checkValid($id)
    {
        global $DB, $IN;
        
        $DB->query("SELECT * FROM dl_regbot WHERE rid='{$id}'");
        if ( !$myrow = $DB->fetch_row() )
        {
            return false;
        }
        if ( $myrow["regKey"] != $IN["regbot"] )
        {
            return false;
        }
        return true;
    }
    
    function showRegBotImg()
    {
        global $DB, $IN;

        if ( $IN['rc'] == "" )
            return false;

        // Get the info from the db
        $DB->query("SELECT * FROM dl_regbot WHERE rid='".trim(addslashes($IN['rc']))."'");
        if ( !$myrow = $DB->fetch_row() )
            return false;

        flush();

        @header("Content-Type: image/jpeg");

        $font_style = 5;
        $content    = $myrow["regKey"];
        $no_chars   = strlen($content);

        $charheight = ImageFontHeight($font_style);
        $charwidth  = ImageFontWidth($font_style);
        $strwidth   = $charwidth * intval($no_chars);
        $strheight  = $charheight;

        $imgwidth   = $strwidth  + 15;
        $imgheight  = $strheight + 15;
        $img_c_x    = $imgwidth  / 2;
        $img_c_y    = $imgheight / 2;

        $im       = ImageCreate($imgwidth, $imgheight);
        $text_col = ImageColorAllocate($im, 255, 0, 0);
        $back_col = ImageColorAllocate($im, 255,255,255);

        ImageFilledRectangle($im, 0, 0, $imgwidth, $imgheight, $back_col);

        $draw_pos_x = $img_c_x - ($strwidth  / 2) + 1;
        $draw_pos_y = $img_c_y - ($strheight / 2) + 1;

        ImageString($im, $font_style, $draw_pos_x, $draw_pos_y, $content, $text_col);

        ImageJPEG($im);
        ImageDestroy($im);

        exit();

    }
}

$loader = new captcha();

?>
