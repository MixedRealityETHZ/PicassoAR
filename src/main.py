from image_processing import *

EXAMPLE_IMAGE = 'me.jpg' # replace with your image
SAVE_PATH = 'examples'

def main():
    sobel_filter(EXAMPLE_IMAGE, SAVE_PATH)


if __name__ == '__main__':
    main()