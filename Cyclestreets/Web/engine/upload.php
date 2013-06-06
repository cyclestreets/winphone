<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/upload.php $
 * $Revision: 50 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-12-20 20:32:52 +0000 (Sat, 20 Dec 2008) $
 *********************************************************/
 
class CUpload
{
	var $file_name,
		$masked_file,
		$temp_name,
		$file_size,
		$real_size,
		$file_type,
		$image_size,
		$gd2,
		$errorMsg;
	
	function CUpload()
	{
		$this->file_name = "";
		$this->masked_file = "";
		$this->file_size = 0;
		$this->real_size = 0;
	}
	
	function uploadImage($fieldname, $size, $doCopyright=1)
	{
		global $std, $rwdInfo, $CONFIG;
		
		$this->file_name = $_FILES[$fieldname]['name'];
		$this->file_type = $_FILES[$fieldname]['type'];
		$this->real_size = $_FILES[$fieldname]['size'];
		
		// If there is no filename then there musn't be a file
		// This also applies if it says "none" [mozilla] and "http://" for an empty file field
		if ($this->file_name == "" || !$this->file_name || 
			$this->file_name == "none" || $this->file_name == "http://" )
		{
			$this->file_name = "";
			return $this;
		}

		// If there is a filesize passed, check uploaded file is within limits
		if ( $size )
		{
			if ($this->real_size > $size)
				$std->error(GETLANG("er_filesize"));
		}
		
		// Remove invalid characters from filename
		$this->file_name = preg_replace( "/[^\w\.]/", "_", $this->file_name );
		
		// Add the extension
		// Opera fix
		$this->file_type = preg_replace( "/^(.+?);.*$/", "\\1", $this->file_type );
				
		switch($this->file_type)
		{
			case 'image/gif':
				$ext = '.gif';
				break;
			case 'image/jpeg':
				$ext = '.jpg';
				break;
			case 'image/pjpeg':
				$ext = '.jpg';
				break;
			case 'image/x-png':
				$ext = '.png';
				break;
            case 'image/png':
                $ext = '.png';
                break;
			default:
				$ext = '.rwd';
				break;
		}
		
		if ( $ext == ".rwd" )
		{
			$std->error( GETLANG("er_notimage") );
			$this->file_name = "";
			return $this;
		}
		
		// Set the image dimensions
		$size = getimagesize($_FILES[$fieldname]['tmp_name']);
		$this->image_size = $size[0]."x".$size[1];
		
		// Move the file from the system temporary folder to the downloads temp folder
		if (! @move_uploaded_file( $_FILES[$fieldname]['tmp_name'], $rwdInfo->path."/temp/".$this->file_name) )
		{
			$std->error(GETLANG("er_upload"));
			$this->file_name = "";
			return $this;
		}
		else
		{
			@chmod( $rwdInfo->path."/temp/".$this->file_name, 0777 );
			if ( $CONFIG['thumb_generate'] != "none" )
				$this->generateThumb($rwdInfo->path."/temp/",$this->file_name, $ext);
		}
		
		if ( $CONFIG['copyright'] and $doCopyright)
		{
			// Add copyright string to image
			$this->copyrightText($rwdInfo->path."/temp/", $this->file_name, $ext);
		}

		return $this;
	}

	function uploadFile($fieldname)
	{
		global $DB, $CONFIG, $std, $rwdInfo;

		$this->file_name = $_FILES[$fieldname]['name'];
		$this->real_size = $_FILES[$fieldname]['size'];
		$this->file_type = $_FILES[$fieldname]['type'];

		// If there is no filename then there musn't be a file
		// This also applies if it says "none" [mozilla] and "http://" for an empty file field
		if ($this->file_name == "" || !$this->file_name ||
			$this->file_name == "none" || $this->file_name == "http://" )
		{
			$this->file_name = "";
			$this->errorMsg = GETLANG("er_nofile");
			return $this;
		}

		// Do our rather elaborate file type permission stuff
		$DB->query("SELECT * FROM dl_filetypes");
		if ( !$myrow = $DB->fetch_row() )   // If no filetypes defines
		{
			if ( !$CONFIG["allowunknown"] )    // And non defined mime types are not allowed
											// Return an error. The admin has clearly lost his marbles
			{
				$this->file_name = "";
				$this->errorMsg = GETLANG("er_invalidtype");
				return $this;
			}
		}
		else
		{
			$found = false;
			do
			{
				// If a matching mime type if found
				if ( $this->file_type == $myrow["mimetype"] )
				{
					$found = true;
					// If upload of this type is allowed
					if ($myrow["allowed"])
					{
						// If the file is larger than allowed then return
						if ( $myrow["maxsize"] < $this->real_size )
						{
							$this->file_name = "";
							$this->errorMsg = GETLANG("er_toobig")."<br>".GETLANG("er_allowedsize").": {$myrow[maxsize]}<br>".GETLANG("filesize").": {$this->real_size}<br>".GETLANG("filetype").": {$this->file_type}<br>";
							return $this;
						}
                        else
                        {
                            break;
                        }
					}
					else
					{
						// This type is not allowed so return an error
						$this->file_name = "";
						$this->errorMsg = GETLANG("er_invalidtype"). ': <font color="red">' . $myrow["mimetype"] . '</font>';
						return $this;
					}
				}
			} while ( $myrow = $DB->fetch_row() );
		}
		// Mime type not found in list
		if ( !$found )
		{
			// if non defined types are not allowed
			if (!$CONFIG["allowunknown"] )
			{
				// This type is not allowed so return an error
				$this->file_name = "";
				$this->errorMsg = GETLANG("er_invalidtype");
				return $this;
			}
		}

		$this->file_size = $std->my_filesize($this->real_size);
		// Remove invalid characters from filename
		$this->file_name = preg_replace( "/[^\w\.]/", "_", $this->file_name );

		// Randomly generate a filename for extra protection from hackers uploading scripts
        if ($CONFIG["encryptFilenames"])
        {
            srand(time());
		    $this->masked_file = md5($this->file_name.rand().time());
        }
        else
        {
            // Encrypting disabled. Do basic file extension change to prevent upload of executable files
            $this->masked_file = $std->strip_ext($this->file_name);
        }
		// Add the extension
		// Opera fix
		$this->file_type = preg_replace( "/^(.+?);.*$/", "\\1", $this->file_type );

		switch($this->file_type)
		{
			case 'image/gif':
				$ext = '.gif';
				break;
			case 'image/jpeg':
				$ext = '.jpg';
				break;
			case 'image/pjpeg':
				$ext = '.jpg';
				break;
			case 'image/x-png':
				$ext = '.png';
				break;
            case 'image/png':
                $ext = '.png';
                break;
			default:
				$ext = '.rwd';
				break;
		}

		$this->masked_file .= $ext;

		if ( $ext == ".gif" || $ext == ".png" || $ext == ".jpg" )
		{
			// Set the image dimensions
			$size = getimagesize($_FILES[$fieldname]['tmp_name']);
			$this->image_size = $size[0]."x".$size[1];
		}
		// Move the file from the temporary folder to the downloads folder
		if (! @move_uploaded_file( $_FILES[$fieldname]['tmp_name'], $rwdInfo->path."/temp/".$this->masked_file) )
		{
			$this->file_name = "";
			$this->errorMsg = GETLANG("er_couldnotmove")."<br>Filename: '".$_FILES[$fieldname]['tmp_name']."'<br>Error code: ".$_FILES[$fieldname]['error']." upload_max_filesize=".ini_get("upload_max_filesize")."<br>";
			return $this;
		}
		else
		{
			@chmod( $rwdInfo->path."/temp/".$this->masked_file, 0777 );
		}

		return $this;
	}

	function moveImage($oldname, $newname)
	{
		global $std, $CONFIG, $rwdInfo;
		// Bit of a hack to make sure we dont try to move a file if no file was sent!
		if ( !$oldname )
			return;
		$oldfile = $rwdInfo->path."/temp/".$oldname;
		$oldthumb = $rwdInfo->path."/temp/t_".$oldname;
		$newfile = $CONFIG['imagesfolder']."/".$newname;
		$newthumb = $CONFIG['imagesfolder']."/t_".$newname;

		// If file already exists then rename
        $counter = 1;
		while (is_file($newfile))
		{
			$newname = $newname."_".$counter;
			$newfile = $CONFIG['imagesfolder']."/".$newname;
			$newthumb = $CONFIG['imagesfolder']."/t_".$newname;#
            $counter++;
		}
		if (!@copy($oldfile, $newfile))
			$std->error("failed to copy $oldfile to $newfile...<br>\n");
		else
			@chmod( $newfile, 0777 );

		// Check if a thumbnail was successfully uploaded
		if(is_file($oldthumb))
		{
			if (!@copy($oldthumb, $newthumb))
				$std->error("failed to copy $oldfile to $newfile...<br>\n");
			else
				@chmod( $newthumb, 0777 );
			if ( !@unlink($oldthumb) )
				$std->error(GETLANG("er_couldnotremove").": ".$oldthumb);
		}
		if ( !@unlink($oldfile) )
			$std->error(GETLANG("er_couldnotremove").": ".$oldfile);

		$this->masked_file = $newname;
		return $this;
	}

	function moveFile($oldname, $newname)
	{
		global $rwdInfo, $CONFIG, $std;
		// Bit of a hack to make sure we dont try to move a file if no file was sent!
		if ( !$oldname )
			return;
		$oldfile = $rwdInfo->path."/temp/".$oldname;
		$newfile = $CONFIG['filesfolder']."/".$newname;

		// If file already exists then rename
		$counter = 1;
		while (is_file($newfile))
		{
			$rename = $newname."_".$counter;
			$newfile = $CONFIG['filesfolder']."/".$rename;
			$counter++;
		}
		if (!@copy($oldfile, $newfile))
		{
    		$std->error("failed to copy $oldfile to $newfile...<br>\n");
		}
		else
		{
			@chmod( $newfile, 0777 );
		}
		if ( !@unlink($oldfile) )
			$std->error(GETLANG("er_couldnotremove").": ".$oldfile." to ".$newfile);
		$this->masked_file = $newname;
		return $this;
	}

	function moveMassUploadFile($filepath, $oldname)
	{
		global $rwdInfo, $CONFIG, $std;
		// Error checking
		if ( !$filepath || !$oldname )
			return;
		$oldfile = $filepath."/".$oldname;

		if (!is_file("/usr/local/apache/conf/mime.types"))
			$mimePath = $rwdInfo->path."/mime.types";
		else
			$mimePath = "/usr/local/apache/conf/mime.types";

		require_once ROOT_PATH."/engine/mime_types.php";
		$mime = new Mime_Types($mimePath);
		$this->file_type = $mime->get_type($oldname);
		$this->real_size = filesize($oldfile);
		$this->file_size = $std->my_filesize($this->real_size);
		$this->file_name = $oldname;
		// Remove invalid characters from filename
		$this->file_name = preg_replace( "/[^\w\.]/", "_", $this->file_name );

		// Randomly generate a filename for extra protection from hackers uploading scripts
        if ($CONFIG["encryptFilenames"])
        {
            srand(time());
            $this->masked_file = md5($oldname.rand().time());
        }
        else
        {
            // Encrypting disabled. Do basic file extension change to prevent upload of executable files
            $this->masked_file = $std->strip_ext($this->file_name);
        }

		switch($this->file_type)
		{
			case 'image/gif':
				$ext = '.gif';
				break;
			case 'image/jpeg':
				$ext = '.jpg';
				break;
			case 'image/pjpeg':
				$ext = '.jpg';
				break;
			case 'image/x-png':
				$ext = '.png';
				break;
			case 'image/png':
                $ext = '.png';
                break;
            default:
				$ext = '.rwd';
				break;
		}

		$this->masked_file .= $ext;

		if ( $ext == ".gif" || $ext == ".png" || $ext == ".jpg" )
		{
			// Set the image dimensions
			$size = getimagesize($oldfile);
			$this->image_size = $size[0]."x".$size[1];
		}

		$newfile = $CONFIG['filesfolder']."/".$this->masked_file;

		// If file already exists then rename
		$counter = 1;
		while (is_file($newfile))
		{
			$newname = $this->masked_file."_".$counter;
			$newfile = $CONFIG['filesfolder']."/".$newname;
			$counter++;
		}
		if (!@copy($oldfile, $newfile))
		{
			$std->error("failed to copy $oldfile to $newfile...<br>\n");
			return;
		}
		else
		{
			@chmod( $newfile, 0777 );
		}
		
		return $this;
	}

	function generateThumb($pathToImg, $file, $ext)
	{
		global $CONFIG, $std;

		if ($CONFIG["thumb_generate"] == "none")
			return;
		$width=$CONFIG["thumbWidth"];
		$height=$CONFIG["thumbHeight"];

		// Check whether we have support for gif
		if ($ext==".gif" and function_exists('imagecreatefromgif'))
			$src_img=imagecreatefromgif($pathToImg.$file);
		else if ($ext==".jpg")
			$src_img=imagecreatefromjpeg($pathToImg.$file);
		else if ($ext==".png") 
			$src_img=@imagecreatefrompng($pathToImg.$file);
		else
			return;
			
		if (!$src_img) 
		{
			$std->error( GETLANG("er_thumbfail")." {$ext} ".GETLANG("er_andpath")." {$pathToImg}{$file}" );
			return;
		}

		// Check image dimensions and resize is necessary
		$old_x=imageSX($src_img);
		$old_y=imageSY($src_img);

		$xScale = $width/$old_x;
		$yScale = $height/$old_y;

        if ( $xScale > 1 and $yScale > 1 )
        {
            return;     // Smaller than thumbnail so don't bother
        }

		if ($xScale < $yScale)
		{
			$thumb_w=$old_x*$xScale;
			$thumb_h=$old_y*$xScale;
		}
		if ($yScale <= $xScale) {
			$thumb_w=$old_x*$yScale;
			$thumb_h=$old_y*$yScale;
		}
		
		// Create the image
		if ($CONFIG["thumb_generate"]=="gd")
		{
			$dst_img=ImageCreate($thumb_w,$thumb_h);
			imagecopyresized($dst_img,$src_img,0,0,0,0,$thumb_w,$thumb_h,$old_x,$old_y);
		}
		else
		{
			$dst_img=ImageCreateTrueColor($thumb_w,$thumb_h);
			imagecopyresampled($dst_img,$src_img,0,0,0,0,$thumb_w,$thumb_h,$old_x,$old_y);
		}
		
		$filename = $pathToImg."t_".$file;
		// Move the file and clean up
		if ($ext == ".png") 
			imagepng($dst_img,$filename); 
		else if ( $ext == ".jpg" )
			imagejpeg($dst_img,$filename); 
		else if ( $ext == ".gif" and function_exists('imagegif') )
			imagegif($dst_img,$filename);
		imagedestroy($dst_img); 
		imagedestroy($src_img); 
	}

    // This also resizes the image based on the max dimensions
	function copyrightText($path, $image, $ext)
	{
		global $CONFIG, $std;
        // Check whether we have support for gif
		if ($ext==".gif" and function_exists('imagecreatefromgif'))
			$src_img=imagecreatefromgif($pathToImg.$file);
		else if ($ext==".jpg")
			$src_img=imagecreatefromjpeg($path.$image);
		else if ($ext==".png") 
			$src_img=@imagecreatefrompng($path.$image);
		else 
			return;

		if (!$src_img)
		{
            if ( $ext == ".gif" )
                return;
			$std->error( GETLANG("er_copyrightfail")." {$ext} ".GETLANG("er_andpath")." {$path}{$image}" );
			return;
		}

        $width=$CONFIG["maxWidth"];
		$height=$CONFIG["maxHeight"];

        // Check image dimensions and resize if necessary
		$old_x=imageSX($src_img);
		$old_y=imageSY($src_img);

		$xScale = $width/$old_x;
		$yScale = $height/$old_y;

		if ($xScale < 1 and $xScale < $yScale)
		{
			$thumb_w=$old_x*$xScale;
			$thumb_h=$old_y*$xScale;
		}
		else if ($yScale < 1 and $yScale < $xScale)
        {
			$thumb_w=$old_x*$yScale;
			$thumb_h=$old_y*$yScale;
		}
        else
        {
            $thumb_w=$old_x;
            $thumb_h=$old_y;
		}
        // Create the image
		if ($CONFIG["thumb_generate"]=="gd")
		{
			$dst_img=ImageCreate($thumb_w,$thumb_h);
			imagecopyresized($dst_img,$src_img,0,0,0,0,$thumb_w,$thumb_h,$old_x,$old_y);
		}
		else
		{
			$dst_img=ImageCreateTrueColor($thumb_w,$thumb_h);
			imagecopyresampled($dst_img,$src_img,0,0,0,0,$thumb_w,$thumb_h,$old_x,$old_y);
		}
		
		$txt_color = ImageColorAllocate ($dst_img, 255, 255, 255); 
    	$textheight = imagefontheight ( 2 );
		//$std->error("text height $textheight");
		ImageString ($dst_img, 2, 5, ($height-5-$textheight), $CONFIG['copystring'], $txt_color);
		//ImageString ($dst_img, 2, 5, 5, $CONFIG['copystring'], $txt_color);
		
		unlink($path.$image);
		$filename = $path.$image;
		// Move the file and clean up
		if ($ext == ".png")
			imagepng($dst_img,$filename); 
		else if ( $ext == ".jpg" )
			imagejpeg($dst_img,$filename); 
        else if ( $ext == ".gif" and function_exists('imagegif') )
			imagegif($dst_img,$filename);
		imagedestroy($dst_img); 
		imagedestroy($src_img); 
	}
}

?>